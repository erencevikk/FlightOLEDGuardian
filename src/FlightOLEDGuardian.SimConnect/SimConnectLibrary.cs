using System.Reflection;
using System.Runtime.InteropServices;

namespace FlightOLEDGuardian.SimConnect;

// Resolve an already installed Microsoft client locally; never download random DLLs.
public static class SimConnectLibrary
{
    private static nint loaded;
    public static string? LoadedPath { get; private set; }
    static SimConnectLibrary() => NativeLibrary.SetDllImportResolver(typeof(SimConnectLibrary).Assembly,Resolve);
    private static nint Resolve(string name,Assembly assembly,DllImportSearchPath? search) => name.Equals("SimConnect.dll",StringComparison.OrdinalIgnoreCase)?loaded:0;

    public static void EnsureLoaded(string? explicitPath=null,bool discoverInstalled=true)
    {
        if(!string.IsNullOrWhiteSpace(explicitPath) && !File.Exists(explicitPath))throw new DllNotFoundException("Selected SimConnect.dll does not exist: "+explicitPath);
        if(loaded!=0)
        {
            if(!string.IsNullOrWhiteSpace(explicitPath) && !string.Equals(Path.GetFullPath(explicitPath),LoadedPath,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Restart Guardian to use a different native SimConnect library.");
            return;
        }
        var candidates=new List<string>();
        if(!string.IsNullOrWhiteSpace(explicitPath))candidates.Add(Path.GetFullPath(explicitPath));
        else if(discoverInstalled)candidates.AddRange(FindCandidates());
        var errors=new List<string>();
        foreach(var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if(!File.Exists(candidate))continue;
            try
            {
                if(!IsX64(candidate)){errors.Add(Path.GetFileName(candidate)+": not x64");continue;}
                nint handle=NativeLibrary.Load(candidate);
                string[] exports=["SimConnect_Open","SimConnect_Close","SimConnect_GetNextDispatch","SimConnect_AddToDataDefinition","SimConnect_RequestDataOnSimObject","SimConnect_SubscribeToSystemEvent","SimConnect_RequestSystemState"];
                if(exports.Any(e=>!NativeLibrary.TryGetExport(handle,e,out _))){NativeLibrary.Free(handle);errors.Add("Required native exports missing: "+candidate);continue;}
                loaded=handle;LoadedPath=Path.GetFullPath(candidate);return;
            }
            catch(Exception e) when(e is DllNotFoundException or BadImageFormatException or IOException or UnauthorizedAccessException){errors.Add(Path.GetFileName(candidate)+": "+e.Message);}
        }
        throw new DllNotFoundException(errors.Count==0?"No installed x64 SimConnect client found. Select the official client DLL in Settings.":string.Join("; ",errors));
    }
    public static IReadOnlyList<string> FindCandidates()
    {
        var paths=new List<string>{Path.Combine(AppContext.BaseDirectory,"SimConnect.dll")};
        foreach(var root in new[]{Environment.GetEnvironmentVariable("MSFS2024_SDK"),Environment.GetEnvironmentVariable("MSFS_SDK"),@"C:\MSFS 2024 SDK",@"C:\MSFS SDK"})
            if(!string.IsNullOrWhiteSpace(root))paths.Add(Path.Combine(root,"SimConnect SDK","lib","SimConnect.dll"));
        foreach(var drive in DriveInfo.GetDrives().Where(d=>d.DriveType==DriveType.Fixed))paths.Add(Path.Combine(drive.Name,"BeyondATC","BeyondATC_Data","Plugins","x86_64","SimConnect.dll"));
        var mobi=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MobiFlight","MobiFlight Connector");
        var python=Path.Combine(mobi,"Python");
        if(Directory.Exists(python))foreach(var version in Directory.EnumerateDirectories(python))paths.Add(Path.Combine(version,"packages","SimConnect","SimConnect.dll"));
        paths.Add(Path.Combine(mobi,"SimConnect.dll"));
        return paths.Where(File.Exists).ToArray();
    }
    public static bool IsX64(string path)
    {
        using var stream=File.OpenRead(path);using var reader=new BinaryReader(stream);
        if(stream.Length<64 || reader.ReadUInt16()!=0x5A4D)return false;
        stream.Position=0x3c;int offset=reader.ReadInt32();if(offset<0 || offset>stream.Length-6)return false;
        stream.Position=offset;return reader.ReadUInt32()==0x00004550 && reader.ReadUInt16()==0x8664;
    }
}
