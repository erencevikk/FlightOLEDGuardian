using System.Threading;
using System.Windows;
using System.Windows.Threading;
using FlightOLEDGuardian.Windows;

namespace FlightOLEDGuardian.App;
public partial class App : Application
{
    private Mutex? mutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var args=Bootstrap.Arguments;
        string data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FlightOLEDGuardian");
        int index=Array.IndexOf(args,"--data-dir");if(index>=0 && index+1<args.Length)data=Path.GetFullPath(args[index+1]);
        Directory.CreateDirectory(data);
        void Trace(string text){if(args.Contains("--smoke-test"))File.AppendAllText(Path.Combine(data,"startup.log"),text+"\n");}
        Trace("Startup entered; acquiring instance mutex");
        mutex=new Mutex(true,args.Contains("--live-flight-test")?@"Local\FlightOLEDGuardian.FlightTest":args.Contains("--smoke-test")?@"Local\FlightOLEDGuardian.SmokeTest":@"Local\FlightOLEDGuardian",out bool first);
        if(!first){Trace("Another instance is running");Shutdown();return;}
        var store=new SettingsStore(data);
        if(args.Contains("--live-flight-test"))
        {
            var actual=new SettingsStore(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FlightOLEDGuardian")).Load();
            store.Save(actual with{AutoLaunch=false,SimBriefIdentity="",LanguageSelectionCompleted=true,OnboardingComplete=true,HideSimBriefIdentity=true});
        }
        int languageIndex=Array.IndexOf(args,"--language");
        if(languageIndex>=0 && languageIndex+1<args.Length)store.Save(store.Load() with{Language=args[languageIndex+1]});
        Trace("Creating main window");
        DispatcherUnhandledException+=(_,args)=>{store.Log(args.Exception);foreach(Window w in Windows.Cast<Window>().ToArray())if(w.Title=="FlightOLED Guardian Protection")w.Close();args.Handled=true;Shutdown(1);};
        var main=new MainWindow(store,args.Contains("--mock"),args.Contains("--watch"));MainWindow=main;
        Trace("Main window constructed");
        new System.Windows.Interop.WindowInteropHelper(main).EnsureHandle();
        var currentSettings=store.Load();
        if(!args.Contains("--watch") && !args.Contains("--tray") || !currentSettings.LanguageSelectionCompleted || !currentSettings.OnboardingComplete)main.Show();
        Trace("Main window shown");
        if(!args.Contains("--smoke-test") && !args.Contains("--live-flight-test"))_ = main.CheckForUpdates();
        if(args.Contains("--live-flight-test"))
        {
            var liveTimer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};
            liveTimer.Tick+=async (_,_)=>
            {
                liveTimer.Stop();
                try{await main.RunFlightChecks(data);main.Exit();}
                catch(Exception error){File.WriteAllText(Path.Combine(data,"flight-test-failure.txt"),error.Message);main.Exit();}
            };
            liveTimer.Start();
        }
        if(args.Contains("--smoke-test"))
        {
            var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};
            timer.Tick+=async (_,_)=>
            {
                timer.Stop();
                try{if(args.Contains("--ui-self-test"))await main.RunUiChecks(data);main.SavePreview(Path.Combine(data,"dashboard.png"));File.WriteAllText(Path.Combine(data,"smoke-test.txt"),"WPF startup, dispatcher and dashboard render succeeded");main.Exit();}
                catch(Exception error){store.Log(error);File.WriteAllText(Path.Combine(data,"ui-failure.txt"),error.ToString());Shutdown(1);}
            };timer.Start();
        }
    }
    protected override void OnExit(ExitEventArgs e){mutex?.Dispose();base.OnExit(e);}
}
