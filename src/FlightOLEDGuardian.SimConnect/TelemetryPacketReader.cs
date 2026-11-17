using System.Buffers.Binary;
using System.Text;
using FlightOLEDGuardian.Core;

namespace FlightOLEDGuardian.SimConnect;

public static class TelemetryPacketReader
{
    public const int PacketLength=40+9*8+256;
    public static FlightTelemetry? Decode(ReadOnlySpan<byte> packet,double now,bool running,bool paused,string sessionId)
    {
        if(packet.Length<PacketLength || BinaryPrimitives.ReadUInt32LittleEndian(packet[8..])!=8 ||
            BinaryPrimitives.ReadUInt32LittleEndian(packet[12..])!=1 || BinaryPrimitives.ReadUInt32LittleEndian(packet[20..])!=1 ||
            BinaryPrimitives.ReadUInt32LittleEndian(packet[36..])!=10)return null;
        double[] values=new double[9];
        for(int i=0;i<values.Length;i++)values[i]=BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(packet[(40+i*8)..]));
        if(values.Any(v=>!double.IsFinite(v)) || values[4] is not (0 or 1) || values[5] is not (0 or 1))return null;
        var title=packet.Slice(112,256);int end=title.IndexOf((byte)0);if(end>=0)title=title[..end];
        return new(){Simulator="Microsoft Flight Simulator / SimConnect",SessionId=sessionId,Aircraft=Encoding.ASCII.GetString(title),
            Connected=true,Running=running,Paused=paused,AltitudeMSL=values[0],AltitudeAGL=values[1],GroundSpeed=values[2],VerticalSpeed=values[3],
            OnGround=values[4]==1,Autopilot=values[5]==1,Latitude=values[6],Longitude=values[7],BankDegrees=values[8],ReceivedAt=now};
    }
}
