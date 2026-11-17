using System.Threading;

namespace FlightOLEDGuardian.App;

public static class Bootstrap
{
    public static string[] Arguments { get; private set; }=[];
    [STAThread]
    public static int Main(string[] args)
    {
        if(string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"FlightOLEDGuardian.FlightTest",StringComparison.OrdinalIgnoreCase))
            args=args.Concat(new[]{"--live-flight-test","--data-dir",Path.Combine(AppContext.BaseDirectory,"FlightTestResults")}).ToArray();
        Arguments=args;
        int dataIndex=Array.IndexOf(args,"--data-dir");
        string? diagnostics=dataIndex>=0 && dataIndex+1<args.Length?Path.GetFullPath(args[dataIndex+1]):null;
        void Trace(string message)
        {
            if(diagnostics==null)return;
            Directory.CreateDirectory(diagnostics);
            File.AppendAllText(Path.Combine(diagnostics,"startup.log"),$"{DateTimeOffset.Now:O} PID={Environment.ProcessId} {message}\n");
        }
        try
        {
            Trace("Creating WPF application");
            var app=new App();
            Trace("Loading application resources");
            app.InitializeComponent();
            Trace("Starting dispatcher");
            return app.Run();
        }
        catch(Exception e)
        {
            Trace("Startup failed: "+e);
            return 1;
        }
    }
}
