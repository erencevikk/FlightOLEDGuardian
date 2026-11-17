using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using FlightOLEDGuardian.Core;
using Microsoft.Win32;

namespace FlightOLEDGuardian.Windows;

public sealed class SettingsStore(string directory)
{
    public string DirectoryPath => directory;
    public string? Warning { get; private set; }
    public GuardianSettings Load()
    {
        try
        {
            var path=Path.Combine(directory,"settings.json");if(!File.Exists(path))return new();
            string json=File.ReadAllText(path);var s=JsonSerializer.Deserialize<GuardianSettings>(json)??new();
            using var document=JsonDocument.Parse(json);
            if(!document.RootElement.TryGetProperty(nameof(GuardianSettings.DefaultsRevision),out var revision) || revision.GetInt32()<3)
            {
                // Migrate the former built-in IFR default; explicit custom MSL profiles remain available.
                if(s.Profile=="IFR / Airliner" && s.Reference==AltitudeReference.MSL && s.ActivationFeet==7000)s=s with{Reference=AltitudeReference.AGL};
                s=s with{DefaultsRevision=3};
            }
            s.Validate();return s;
        }
        catch(Exception e) when(e is IOException or JsonException or ArgumentException or UnauthorizedAccessException) { Warning=$"Settings could not be loaded: {e.Message}";return new(); }
    }
    public void Save(GuardianSettings settings)
    {
        settings.Validate();Directory.CreateDirectory(directory);var path=Path.Combine(directory,"settings.json");
        File.WriteAllText(path+".tmp",JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true})); File.Move(path+".tmp",path,true);
    }
    public void Log(Exception e) { try{Directory.CreateDirectory(directory);File.AppendAllText(Path.Combine(directory,"guardian.log"),$"{DateTimeOffset.Now:O} {e}\n");}catch(IOException){}catch(UnauthorizedAccessException){} }
}

public sealed class InputActivityService
{
    private uint lastInput;
    private readonly Dictionary<uint,Joystick> joysticks=new();
    public bool Changed()
    {
        var input=new LastInput{Size=(uint)Marshal.SizeOf<LastInput>()};
        if(!GetLastInputInfo(ref input)) return true; // Cannot observe input: fail open.
        bool changed=input.Time!=lastInput; lastInput=input.Time;
        for(uint id=0;id<Math.Min(joyGetNumDevs(),16);id++)
        {
            var state=new Joystick{Size=(uint)Marshal.SizeOf<Joystick>(),Flags=255};
            if(joyGetPosEx(id,ref state)!=0) {joysticks.Remove(id);continue;}
            if(joysticks.TryGetValue(id,out var previous))
            {
                bool axis=Math.Abs((long)state.X-previous.X)>1500 || Math.Abs((long)state.Y-previous.Y)>1500 || Math.Abs((long)state.Z-previous.Z)>1500 || Math.Abs((long)state.R-previous.R)>1500 || Math.Abs((long)state.U-previous.U)>1500 || Math.Abs((long)state.V-previous.V)>1500;
                if(axis || state.Buttons!=previous.Buttons || state.Pov!=previous.Pov) {changed=true;joysticks[id]=state;}
            }
            else joysticks[id]=state;
        }
        return changed;
    }
    [StructLayout(LayoutKind.Sequential)] private struct LastInput { public uint Size,Time; }
    [StructLayout(LayoutKind.Sequential)] private struct Joystick { public uint Size,Flags,X,Y,Z,R,U,V,Buttons,ButtonNumber,Pov,Reserved1,Reserved2; }
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInput value);
    [DllImport("winmm.dll")] private static extern uint joyGetNumDevs();
    [DllImport("winmm.dll")] private static extern uint joyGetPosEx(uint id,ref Joystick value);
}

public static class CaptureCompatibility
{
    public static bool Allowed(GuardianSettings s) => !s.Streaming || !s.DisableUnverifiedCapture || s.CaptureVerified && s.CaptureMethod!=CaptureMethod.CaptureCard;
    public static string Describe(GuardianSettings s) => !s.Streaming ? "Streaming mode off" : s.CaptureMethod==CaptureMethod.CaptureCard ? "HDMI / cloned output includes the overlay. Use a separate unprotected output; no hardware compatibility verified." : s.CaptureVerified ? "User-confirmed capture setup. Re-test after changing OBS or display settings." : "Not capture-tested. Record a sample in OBS before enabling protection while streaming.";
}

public static class AutoLaunchService
{
    // Only called by an explicit user choice, never during a build or test.
    public static void SetEnabled(bool enabled)
    {
        using var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if(enabled)key.SetValue("FlightOLEDGuardian",BuildCommand(Path.Combine(AppContext.BaseDirectory,"FlightOLEDGuardian.App.exe")));else key.DeleteValue("FlightOLEDGuardian",false);
    }
    public static string BuildCommand(string executable)
    {
        if(!Path.IsPathFullyQualified(executable) || executable.Contains('"'))throw new ArgumentException("A fully qualified app executable is required");
        return $"\"{executable}\" --watch";
    }
    public static bool SimulatorRunning(IEnumerable<string> configuredNames)
    {
        foreach(var name in configuredNames.Where(n=>!string.IsNullOrWhiteSpace(n)))
        {foreach(var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name)))using(process)return true;}
        return false;
    }
}
