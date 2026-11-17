using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Threading.Tasks;
using FlightOLEDGuardian.Windows;
using Forms=System.Windows.Forms;

namespace FlightOLEDGuardian.App;
public partial class MainWindow : Window
{
    private readonly GuardianViewModel vm;
    private readonly DispatcherTimer timer;
    private readonly Forms.NotifyIcon tray;
    private bool exiting;
    private bool onboardingInProgress;
    private nint hwnd;
    public MainWindow(SettingsStore store,bool mock,bool watch)
    {
        bool smoke=Bootstrap.Arguments.Contains("--smoke-test");
        void Trace(string message){if(smoke)File.AppendAllText(Path.Combine(store.DirectoryPath,"startup.log"),message+"\n");}
        Trace("Loading main window XAML");InitializeComponent();Trace("Creating view model");vm=new(store,mock,watch);DataContext=vm;
        Trace("Creating tray icon");
        using(var iconStream=Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Guardian.ico"))!.Stream)
            tray=new(){Text="FlightOLED Guardian",Icon=new System.Drawing.Icon(iconStream),Visible=true};
        UpdateTrayLanguage();UiText.Changed+=UpdateTrayLanguage;tray.DoubleClick+=(_,_)=>Restore();
        vm.Notice+=text=>{tray.BalloonTipTitle="FlightOLED Guardian";tray.BalloonTipText=text;tray.ShowBalloonTip(8000);};
        Trace("Tray created; starting update timer");
        timer=new(){Interval=TimeSpan.FromMilliseconds(100)};timer.Tick+=(_,_)=>vm.Tick();timer.Start();
        SourceInitialized+=(_,_)=>{Trace("Registering emergency hotkey");hwnd=new WindowInteropHelper(this).Handle;HwndSource.FromHwnd(hwnd)?.AddHook(Hook);if(RegisterHotKey(hwnd,1,0x4003,0x7B))vm.SetHotkeyStatus(true);else vm.SetHotkeyStatus(RegisterHotKey(hwnd,1,0x4006,0x7B),"Ctrl + Shift + F12");Trace("Window source initialized");};
        Closing+=ClosingWindow;
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=OnDisplayChanged;Microsoft.Win32.SystemEvents.PowerModeChanged+=OnPowerChanged;
        Loaded+=(_,_)=>
        {
            if((!vm.Settings.LanguageSelectionCompleted || !vm.Settings.OnboardingComplete) && !onboardingInProgress && !Bootstrap.Arguments.Contains("--smoke-test") && !Bootstrap.Arguments.Contains("--live-flight-test"))
            {
                onboardingInProgress=true;bool wasSuspended=vm.Scheduler.Suspended;vm.Scheduler.Suspend(true);
                try
                {
                if(!vm.Settings.LanguageSelectionCompleted)
                {
                var languagePicker=new Window{Title="Language / Dil",Owner=this,Width=360,Height=180,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=new SolidColorBrush(Color.FromRgb(12,18,27)),Foreground=Brushes.White};
                var choices=new System.Windows.Controls.StackPanel{Margin=new Thickness(24)};
                choices.Children.Add(new System.Windows.Controls.TextBlock{Text="Select language / Dil seçin",FontSize=20,Margin=new Thickness(0,0,0,12)});
                var buttons=new System.Windows.Controls.StackPanel{Orientation=System.Windows.Controls.Orientation.Horizontal};
                foreach(var option in new[]{("en","English"),("tr","Türkçe")}){var button=new System.Windows.Controls.Button{Content=option.Item2,IsDefault=option.Item1=="en"};button.Click+=(_,_)=>{vm.CompleteLanguageSelection(option.Item1);languagePicker.DialogResult=true;};buttons.Children.Add(button);}
                choices.Children.Add(buttons);languagePicker.Content=choices;if(languagePicker.ShowDialog()!=true)return;
                }
                if(!vm.Settings.OnboardingComplete)
                {
                var answer=MessageBox.Show(this,UiText.Get("Start Guardian automatically when Microsoft Flight Simulator starts?\n\nThis adds Guardian to MSFS EXE.xml. Existing addons are preserved and a backup is created. You can change this in Settings."),UiText.Get("Welcome to FlightOLED Guardian"),MessageBoxButton.YesNo,MessageBoxImage.Question);
                vm.CompleteOnboarding(answer==MessageBoxResult.Yes);
                }
                }
                finally{onboardingInProgress=false;vm.Scheduler.Suspend(wasSuspended);}
            }
        };
    }
    private void UpdateTrayLanguage()
    {
        Language=System.Windows.Markup.XmlLanguage.GetLanguage(UiText.Language=="tr"?"tr-TR":"en-US");
        var old=tray.ContextMenuStrip;var menu=new Forms.ContextMenuStrip();
        menu.Items.Add(UiText.Get("Open Guardian"),null,(_,_)=>Restore());menu.Items.Add(UiText.Get("Wake / reset"),null,(_,_)=>vm.Wake());menu.Items.Add(UiText.Get("Skip once"),null,(_,_)=>vm.SkipCommand.Execute(null));menu.Items.Add(UiText.Get("Snooze 10 minutes"),null,(_,_)=>vm.SnoozeCommand.Execute(null));menu.Items.Add(UiText.Get("Suspend / resume"),null,(_,_)=>vm.SuspendCommand.Execute(null));menu.Items.Add(UiText.Get("Exit"),null,(_,_)=>Exit());tray.ContextMenuStrip=menu;old?.Dispose();
    }
    public Task CheckForUpdates()=>vm.CheckUpdatesAsync();
    private void OnDisplayChanged(object? sender,EventArgs e)=>Dispatcher.BeginInvoke(vm.DisplayChanged);
    private void OnPowerChanged(object sender,Microsoft.Win32.PowerModeChangedEventArgs e)=>Dispatcher.BeginInvoke(vm.Emergency);
    private nint Hook(nint h,int message,nint w,nint l,ref bool handled){if(message==0x0312){vm.Emergency();handled=true;}return 0;}
    private void ToTray(object sender,RoutedEventArgs e)=>Hide();
    public void SavePreview(string path)
    {
        UpdateLayout();var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(path);encoder.Save(file);
    }
    // In-app acceptance checks run only with the explicit development CLI switch.
    public async Task RunUiChecks(string directory)
    {
        void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        IEnumerable<T> Descendants<T>(DependencyObject parent) where T:DependencyObject
        {
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(parent);i++)
            {
                var child=VisualTreeHelper.GetChild(parent,i);if(child is T found)yield return found;
                foreach(var nested in Descendants<T>(child))yield return nested;
            }
        }
        vm.TodAlerts=false;vm.TodSound=false;vm.SaveCommand.Execute(null);
        vm.UiLanguage="tr";await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Require(((System.Windows.Controls.TabItem)MainTabs.Items[0]).Header?.ToString()=="Gösterge paneli","Turkish header missing");
        SavePreview(Path.Combine(directory,"dashboard-tr.png"));
        vm.UiLanguage="en";await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Require(((System.Windows.Controls.TabItem)MainTabs.Items[0]).Header?.ToString()=="Dashboard","English header missing");
        Require(vm.TodText.Contains("MOCK / simulated TOD"),"Dynamic TOD did not switch to English");
        SavePreview(Path.Combine(directory,"dashboard-en.png"));
        vm.UiLanguage="tr";MainTabs.SelectedIndex=1;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        var combos=Descendants<System.Windows.Controls.ComboBox>(MainTabs).ToArray();
        Require(combos.Length>=4,"Protection dropdowns missing");
        var profile=combos.First(c=>c.Items.Count==3 && c.Items[0] is UiChoice choice && Equals(choice.Value,"IFR / Airliner"));
        Require(Equals(profile.SelectedValue,"IFR / Airliner"),"Profile selection lost on language switch");
        Require(combos.Any(c=>c.Items.Count>0 && c.Items[0] is string && c.SelectedIndex>=0 && c.SelectedItem is string selected && !string.IsNullOrWhiteSpace(selected)),"Monitor selection lost on language switch");
        profile.IsDropDownOpen=true;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);UpdateLayout();
        var item=(System.Windows.Controls.ComboBoxItem)profile.ItemContainerGenerator.ContainerFromIndex(0);
        Require(item!=null,"Dropdown item not generated");
        Require(item!.Foreground is SolidColorBrush fg && fg.Color.R>180,"Dropdown text too dark");
        Require(item.Background is SolidColorBrush bg && bg.Color.R<80,"Dropdown background too light");
        SavePreview(Path.Combine(directory,"protection-tr.png"));
        var popup=(System.Windows.Controls.Primitives.Popup)profile.Template.FindName("PART_Popup",profile);
        var child=(FrameworkElement)popup.Child;child.UpdateLayout();
        var image=new RenderTargetBitmap(Math.Max(1,(int)child.ActualWidth),Math.Max(1,(int)child.ActualHeight),96,96,PixelFormats.Pbgra32);image.Render(child);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(image));using(var file=File.Create(Path.Combine(directory,"dropdown-tr.png")))png.Save(file);
        profile.IsDropDownOpen=false;
        MainTabs.SelectedIndex=2;vm.MockAltitude=12000;vm.MockTod=42.5;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        var fields=Descendants<System.Windows.Controls.TextBox>(MainTabs).ToArray();
        Require(fields.Any(t=>System.Windows.Data.BindingOperations.GetBinding(t,System.Windows.Controls.TextBox.TextProperty)?.Path.Path==nameof(vm.MockAltitude) && t.Text.Contains("12000")),"Altitude numeric binding failed");
        Require(Descendants<System.Windows.Controls.TextBlock>(MainTabs).Any(t=>t.Text.Contains("42,5 NM")||t.Text.Contains("42.5 NM")),"TOD numeric label missing");
        SavePreview(Path.Combine(directory,"mock-tr.png"));
        MainTabs.SelectedIndex=5;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);SavePreview(Path.Combine(directory,"settings-tr.png"));
        MainTabs.SelectedIndex=0;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        const string privateTestValue="QA-PRIVATE-PILOT-84721";vm.SimBriefIdentity=privateTestValue;vm.HideSimBriefIdentity=false;vm.Streaming=true;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Require(vm.IsSimBriefMasked,"Streaming mode exposed identity");
        Require(!Descendants<System.Windows.Controls.TextBox>(this).Any(t=>t.Text.Contains(privateTestValue)),"Plain identity remained in visual tree");
        Require(Descendants<MaskedIdentityBox>(this).Any(p=>p.IsMasked && p.Value==privateTestValue),"Masked field lost import identity");
        SavePreview(Path.Combine(directory,"privacy-en.png"));
        vm.Streaming=false;vm.HideSimBriefIdentity=true;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Require(!Descendants<System.Windows.Controls.TextBox>(this).Any(t=>t.Text.Contains(privateTestValue)),"Hide preference exposed identity");
        vm.HideSimBriefIdentity=false;await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Require(Descendants<System.Windows.Controls.TextBox>(this).Any(t=>t.Text==privateTestValue),"Reveal outside streaming failed");
        vm.HideSimBriefIdentity=true;vm.SimBriefIdentity="";
        Require(new FlightOLEDGuardian.Core.GuardianSettings().Language=="en" && !new FlightOLEDGuardian.Core.GuardianSettings().LanguageSelectionCompleted,"First-run language default failed");
        Require(vm.Reference==FlightOLEDGuardian.Core.AltitudeReference.AGL,"AGL default missing");
        File.WriteAllText(Path.Combine(directory,"ui-checks.txt"),"PASS Turkish/English live switch and persistence\nPASS dark dropdown foreground/background\nPASS mock altitude numeric binding\nPASS mock TOD numeric label\nPASS streamer masking removes plaintext fields\nPASS reveal works only outside streaming\nPASS English default and separate first-run choice\nPASS AGL activation default\n");
    }
    public async Task RunFlightChecks(string directory)
    {
        var samples=new List<FlightOLEDGuardian.Core.FlightTelemetry>();
        for(int i=0;i<25 && samples.Count<8;i++)
        {
            await Task.Delay(1000);
            if(vm.Flight.Valid)samples.Add(vm.Flight);
        }
        if(samples.Count==0)throw new InvalidOperationException("No valid live MSFS telemetry received. "+vm.Status);
        var flight=samples[^1];var settings=vm.Settings;
        // A separate accelerated clock validates decisions against the actual live sample.
        var engine=new FlightOLEDGuardian.Core.ProtectionScheduler(settings with{AltitudeConfirmationSeconds=0,Reference=FlightOLEDGuardian.Core.AltitudeReference.AGL,Mode=FlightOLEDGuardian.Core.ProtectionMode.PeriodicBlack});
        var initial=engine.Tick(0,settings.IdleSeconds,flight with{ReceivedAt=0});
        bool expectedEligible=!flight.OnGround && flight.AltitudeAGL>=settings.ActivationFeet && (!settings.SuppressManeuvers || Math.Abs(flight.BankDegrees)<15 && Math.Abs(flight.VerticalSpeed)<1500);
        if(engine.Eligible!=expectedEligible)throw new InvalidOperationException("Live AGL eligibility did not match the measured altitude.");
        bool cycle=false;
        if(expectedEligible)
        {
            engine.Tick(settings.NormalSeconds-settings.CountdownSeconds,settings.IdleSeconds+settings.NormalSeconds,flight with{ReceivedAt=settings.NormalSeconds-settings.CountdownSeconds});
            engine.Tick(settings.NormalSeconds,settings.IdleSeconds+settings.NormalSeconds,flight with{ReceivedAt=settings.NormalSeconds});
            if(engine.State!=FlightOLEDGuardian.Core.ProtectionState.Black)throw new InvalidOperationException("Accelerated live-sample cycle failed.");
            double end=settings.NormalSeconds+settings.BlackSeconds;engine.Tick(end,settings.IdleSeconds+end,flight with{ReceivedAt=end});cycle=engine.CompletedCycles==1;
        }
        bool countdownSeen=false,blackSeen=false,physicalCycle=false;
        string physicalResult="Skipped: require stable AP cruise, emergency shortcut and supported capture setup.";
        if(expectedEligible && flight.Autopilot && Math.Abs(flight.BankDegrees)<10 && Math.Abs(flight.VerticalSpeed)<1000 && vm.EmergencyShortcutAvailable && CaptureCompatibility.Allowed(settings))
        {
            int previousCycles=vm.Scheduler.CompletedCycles;
            try
            {
                vm.Scheduler.Configure(settings with{IdleSeconds=0,NormalSeconds=3,BlackSeconds=2,CountdownSeconds=3,AltitudeConfirmationSeconds=0,Mode=FlightOLEDGuardian.Core.ProtectionMode.PeriodicBlack});
                var watch=System.Diagnostics.Stopwatch.StartNew();
                while(watch.Elapsed.TotalSeconds<12 && vm.Scheduler.CompletedCycles==previousCycles)
                {
                    await Task.Delay(100);
                    countdownSeen|=vm.Scheduler.State==FlightOLEDGuardian.Core.ProtectionState.Countdown;
                    blackSeen|=vm.Scheduler.State==FlightOLEDGuardian.Core.ProtectionState.Black;
                }
                physicalCycle=vm.Scheduler.CompletedCycles>previousCycles;
                physicalResult=physicalCycle?"Completed 3s countdown + 2s black on live telemetry.":"Interrupted by input or changed flight conditions.";
            }
            finally{vm.Wake();vm.Scheduler.Configure(settings);vm.Scheduler.Suspend(true);}
        }
        var result=new{Utc=DateTimeOffset.UtcNow,LiveSamples=samples.Count,flight.Simulator,flight.Aircraft,flight.AltitudeMSL,flight.AltitudeAGL,flight.GroundSpeed,flight.Autopilot,flight.OnGround,Reference="AGL",ActivationFeet=settings.ActivationFeet,ExpectedEligible=expectedEligible,InitialState=initial.ToString(),AcceleratedLiveSampleCycle=cycle,CountdownSeen=countdownSeen,BlackSeen=blackSeen,PhysicalCycleCompleted=physicalCycle,PhysicalResult=physicalResult,FlightControlsWritten=false};
        File.WriteAllText(Path.Combine(directory,"flight-test.json"),System.Text.Json.JsonSerializer.Serialize(result,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
    }
    private void Restore(){Show();WindowState=WindowState.Normal;Activate();}
    private void ClosingWindow(object? sender,CancelEventArgs e){if(!exiting){e.Cancel=true;Hide();}}
    public void Exit()
    {
        if(exiting)return;exiting=true;timer.Stop();UnregisterHotKey(hwnd,1);UiText.Changed-=UpdateTrayLanguage;Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=OnDisplayChanged;Microsoft.Win32.SystemEvents.PowerModeChanged-=OnPowerChanged;vm.Dispose();tray.Visible=false;tray.Dispose();Close();Application.Current.Shutdown();
    }
    private void ExportReport(object sender,RoutedEventArgs e)
    {
        if(vm.LatestReport==null){MessageBox.Show(this,UiText.Language=="tr"?"Rapor kaydetmeden önce gerçek veya test uçuşunu tamamla.":"Complete a real or mock flight before exporting a report.");return;}
        var dimensions=((string)((System.Windows.Controls.Button)sender).Tag).Split(',').Select(int.Parse).ToArray();
        var dialog=new Microsoft.Win32.SaveFileDialog{FileName=$"FlightOLED-{vm.LatestReport.TouchdownUtc:yyyyMMdd-HHmm}-{dimensions[0]}x{dimensions[1]}.png",Filter="PNG image|*.png"};if(dialog.ShowDialog()!=true)return;
        try{ReportCardRenderer.Export(vm.LatestReport,dimensions[0],dimensions[1],dialog.FileName,vm.UiLanguage);}catch(IOException ex){MessageBox.Show(this,ex.Message);}
    }
    [DllImport("user32.dll",SetLastError=true)] private static extern bool RegisterHotKey(nint h,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(nint h,int id);
}
