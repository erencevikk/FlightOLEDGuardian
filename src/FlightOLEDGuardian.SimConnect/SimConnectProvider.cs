using System.Runtime.InteropServices;
using FlightOLEDGuardian.Core;

namespace FlightOLEDGuardian.SimConnect;

// Native x64 client. No aircraft writes/events: only telemetry and system subscriptions.
public sealed class SimConnectProvider(string? dllPath=null,bool discoverInstalled=true) : IFlightSimulatorProvider
{
    private nint handle;
    private double nextAttempt;
    private FlightTelemetry latest = new();
    private bool running, paused;
    private string sessionId=Guid.NewGuid().ToString("N");
    public string Status { get; private set; } = "Waiting for MSFS / SimConnect.dll";
    public FlightTelemetry Poll(double now)
    {
        try
        {
            if (handle == 0)
            {
                if (now < nextAttempt) return latest with { Connected=false };
                nextAttempt=now+5;
                sessionId=Guid.NewGuid().ToString("N");
                SimConnectLibrary.EnsureLoaded(dllPath,discoverInstalled);
                Check(Native.Open(out handle,"FlightOLED Guardian",0,0,0,uint.MaxValue)); // SIMCONNECT_OPEN_CONFIGINDEX_LOCAL
                string[] names=["PLANE ALTITUDE","PLANE ALT ABOVE GROUND","GROUND VELOCITY","VERTICAL SPEED","SIM ON GROUND","AUTOPILOT MASTER","PLANE LATITUDE","PLANE LONGITUDE","PLANE BANK DEGREES"];
                string[] units=["feet","feet","knots","feet per minute","Bool","Bool","degrees","degrees","degrees"];
                for(int i=0;i<names.Length;i++)Check(Native.Add(handle,1,names[i],units[i],4,0,uint.MaxValue)); // FLOAT64 = 4
                Check(Native.Add(handle,1,"TITLE",null,9,0,uint.MaxValue)); // STRING256 = 9
                Check(Native.Request(handle,1,1,0,4,0,0,0,0)); // USER, SECOND
                Check(Native.Subscribe(handle,10,"Pause"));
                Check(Native.Subscribe(handle,11,"Sim"));
                Check(Native.Subscribe(handle,13,"FlightLoaded"));
                Check(Native.SystemState(handle,12,"Sim"));
                Status="SimConnect connected; waiting for telemetry";
            }
            for(int i=0;i<128;i++)
            {
                int hr=Native.Next(handle,out var packet,out uint size); if(hr<0)break;
                if(size<12)continue;
                uint id=(uint)Marshal.ReadInt32(packet,8);
                if(id==3) { Disconnect("MSFS closed"); break; }
                if(id==1 && size>=16) { Disconnect($"SimConnect exception {Marshal.ReadInt32(packet,12)}"); break; }
                if(id==4 && size>=24)
                {
                    int eventId=Marshal.ReadInt32(packet,16), data=Marshal.ReadInt32(packet,20);
                    if(eventId==10)paused=data!=0;
                    if(eventId==11)running=data!=0;
                }
                if(id==6)sessionId=Guid.NewGuid().ToString("N"); // EVENT_FILENAME / FlightLoaded
                if(id==15 && size>=24 && Marshal.ReadInt32(packet,12)==12) running=Marshal.ReadInt32(packet,16)!=0;
                if(id==8 && size>=40+9*8+256 && Marshal.ReadInt32(packet,12)==1)
                {
                    byte[] bytes=new byte[TelemetryPacketReader.PacketLength];Marshal.Copy(packet,bytes,0,bytes.Length);
                    var decoded=TelemetryPacketReader.Decode(bytes,now,running,paused,sessionId);
                    if(decoded!=null){latest=decoded;Status="SimConnect • live telemetry";}
                    else {latest=latest with{Connected=false};Status="SimConnect packet rejected: invalid telemetry schema";}
                }
            }
            return latest with { Running=running,Paused=paused };
        }
        catch(Exception e) when(e is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException or COMException or IOException or UnauthorizedAccessException or InvalidOperationException)
        { Disconnect(e is COMException && e.HResult==unchecked((int)0x80004005)?"SimConnect DLL loaded; waiting for a running MSFS connection":"SimConnect: "+e.Message); return latest; }
    }
    private void Disconnect(string reason) { if(handle!=0)Native.Close(handle);handle=0;running=false;latest=latest with{Connected=false};Status=reason; }
    private static void Check(int hr) { if(hr<0)Marshal.ThrowExceptionForHR(hr); }
    public void Dispose() { Disconnect("Disconnected"); }
    private static class Native
    {
        [DllImport("SimConnect.dll",EntryPoint="SimConnect_Open",CharSet=CharSet.Ansi)] internal static extern int Open(out nint h,string name,nint hwnd,uint msg,nint evt,uint config);
        [DllImport("SimConnect.dll",EntryPoint="SimConnect_Close")] internal static extern int Close(nint h);
        [DllImport("SimConnect.dll",EntryPoint="SimConnect_AddToDataDefinition",CharSet=CharSet.Ansi)] internal static extern int Add(nint h,uint id,string name,string? units,uint type,float epsilon,uint datum);
        [DllImport("SimConnect.dll",EntryPoint="SimConnect_RequestDataOnSimObject")] internal static extern int Request(nint h,uint request,uint definition,uint objectId,uint period,uint flags,uint origin,uint interval,uint limit);
        [DllImport("SimConnect.dll",EntryPoint="SimConnect_GetNextDispatch")] internal static extern int Next(nint h,out nint data,out uint size);
        [DllImport("SimConnect.dll",EntryPoint="SimConnect_SubscribeToSystemEvent",CharSet=CharSet.Ansi)] internal static extern int Subscribe(nint h,uint id,string name);
        [DllImport("SimConnect.dll",EntryPoint="SimConnect_RequestSystemState",CharSet=CharSet.Ansi)] internal static extern int SystemState(nint h,uint id,string name);
    }
}
