using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FlightOLEDGuardian.Core;
using FlightOLEDGuardian.Integrations;
using FlightOLEDGuardian.Reporting;
using FlightOLEDGuardian.SimConnect;
using FlightOLEDGuardian.Windows;

namespace FlightOLEDGuardian.App;

public sealed class RelayCommand(Action execute) : ICommand
{
    public bool CanExecute(object? parameter)=>true;
    public void Execute(object? parameter)=>execute();
    public event EventHandler? CanExecuteChanged { add{} remove{} }
}
public sealed class AsyncRelayCommand(Func<Task> execute) : ICommand
{
    private bool running;
    public bool CanExecute(object? parameter)=>!running;
    public async void Execute(object? parameter)
    {
        if(running)return;running=true;CanExecuteChanged?.Invoke(this,EventArgs.Empty);
        try{await execute();}finally{running=false;CanExecuteChanged?.Invoke(this,EventArgs.Empty);}
    }
    public event EventHandler? CanExecuteChanged;
}
public sealed class UiChoice(object value,string label) : INotifyPropertyChanged
{
    public object Value { get; }=value;
    public string Label { get; private set; }=label;
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Rename(string label){Label=label;PropertyChanged?.Invoke(this,new(nameof(Label)));}
}

public sealed class GuardianViewModel : INotifyPropertyChanged,IDisposable
{
    private readonly SettingsStore store;
    private readonly ReportStore reports;
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly InputActivityService input=new();
    private readonly MockTelemetryProvider mock=new();
    private readonly SimBriefClient simbrief=new();
    private readonly CancellationTokenSource lifetime=new();
    private IFlightSimulatorProvider provider;
    private FlightRecorder recorder=new();
    private readonly TodAlert todAlert=new();
    private PlannedRoute? plan;
    private double lastInput, messageUntil, watchCheckedAt=-10;
    private bool accelerated, hotkeyAvailable=true, watchedSimulatorRunning, fetching, disposed;
    private readonly bool watcher;
    private string selectedProfile="IFR / Airliner",language="en",simBriefIdentity="",exeXmlPath="",dllPath="",operationMessage="";
    private bool streaming,hideSimBriefIdentity=true;
    private double mockAltitude=35000,mockTod=185;
    private string? previousAircraft,previousSession;
    private object? speechVoice;
    private bool todIsClose;
    private TodReading lastTod=new(null,"Unavailable","none","No verified FMS or planned TOD data");
    private readonly UiChoice[] profileChoices=new[]{"IFR / Airliner","VFR / General aviation","Custom"}.Select(p=>new UiChoice(p,p)).ToArray();
    private readonly UiChoice[] modeChoices=Enum.GetValues<ProtectionMode>().Select(m=>new UiChoice(m,m.ToString())).ToArray();
    private readonly UiChoice[] captureChoices=Enum.GetValues<CaptureMethod>().Select(m=>new UiChoice(m,m.ToString())).ToArray();
    private readonly UiChoice[] languageChoices=[new("tr","Türkçe"),new("en","English")];
    private string[] monitorNames=[];
    private int monitorIndex;
    public GuardianSettings Settings { get; private set; }
    public ProtectionScheduler Scheduler { get; }
    public OverlayManager Overlay { get; }=new();
    public FlightTelemetry Flight { get; private set; }=new();
    public FlightReport? LatestReport { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<string>? Notice;
    private string T(string text)=>UiText.Get(text,UiLanguage);
    private string Choose(string tr,string en)=>UiLanguage=="tr"?tr:en;
    public string UiLanguage
    {
        get=>language;
        set
        {
            if(value is not ("tr" or "en") || language==value)return;
            language=value;Settings=Settings with{Language=value};UiText.Apply(value);RefreshChoiceLabels();
            try{store.Save(store.Load() with{Language=value});}catch(Exception e)when(e is IOException or UnauthorizedAccessException){store.Log(e);}
            RefreshHistory();Notify("");
        }
    }
    public UiChoice[] LanguageChoices=>languageChoices;
    public UiChoice[] ProfileChoices=>profileChoices;
    public UiChoice[] ModeChoices=>modeChoices;
    public UiChoice[] CaptureChoices=>captureChoices;
    public AltitudeReference[] References=>Enum.GetValues<AltitudeReference>();
    public string EmergencyShortcut { get; private set; }="Ctrl + Alt + F12";
    public bool EmergencyShortcutAvailable=>hotkeyAvailable;
    public string EmergencyHelp=>Choose($"Acil kısayol: {EmergencyShortcut} • Klavye, fare veya belirgin joystick hareketi ekranı açar.",$"Emergency: {EmergencyShortcut} • Any keyboard, mouse or significant joystick input wakes the screen.");
    public string StateText=>T(Scheduler.State.ToString());
    public string ProtectionReason=>!Flight.Connected?Choose("Canlı telemetri bekleniyor; ekran karartılmaz.","Waiting for live telemetry; the screen stays visible."):
        !hotkeyAvailable?Choose("Acil kısayol kullanılamıyor. Diğer Guardian örneğini kapatıp yeniden açın.","Emergency shortcut unavailable. Close other Guardian instances and restart."):
        Scheduler.Suspended?Choose("Koruma durduruldu. Devam etmek için Durdur / devam et düğmesini kullanın.","Protection is suspended. Use Suspend / resume to continue."):
        !Flight.Valid?Choose("Simülatör duraklatılmış veya telemetri geçersiz.","Simulator paused or telemetry unavailable."):
        Flight.OnGround?Choose("Uçak yerde; otomatik koruma beklemede.","Aircraft on ground; automatic protection is waiting."):
        !CaptureCompatibility.Allowed(Settings)?Choose("Yayın düzeni doğrulanmadı; Yayın sekmesindeki ayarları kontrol edin.","Capture setup unverified; check the Streaming tab."):
        Scheduler.State==ProtectionState.Ineligible?Choose($"{Settings.ActivationFeet:0} FT {Settings.Reference} aktivasyon koşulu bekleniyor.",$"Waiting for the {Settings.ActivationFeet:0} FT {Settings.Reference} activation condition."):
        Scheduler.State==ProtectionState.WaitingForIdle?Choose("Kullanıcı etkinliği var; hareketsizlik süresi bekleniyor.","User activity detected; waiting for the idle threshold."):"";
    public string TimerText=>Scheduler.State is ProtectionState.Disconnected or ProtectionState.Ineligible or ProtectionState.WaitingForIdle or ProtectionState.Suspended?Choose("Zamanlayıcı beklemede","Timer waiting"):double.IsPositiveInfinity(Scheduler.RemainingSeconds)?Choose("Tam koruma • herhangi bir girişle ekran açılır","Full protection • wake with any input"):$"{Choose("Sonraki geçiş","Next transition")}: {TimeSpan.FromSeconds(Math.Max(0,Scheduler.RemainingSeconds)):mm\\:ss}";
    public string EligibilityText=>$"{T(Settings.Profile)} · {Settings.ActivationFeet:0} FT {Settings.Reference} · {Choose("Hareketsiz","Idle")} {Math.Max(0,clock.Elapsed.TotalSeconds-lastInput):0}s / {Settings.IdleSeconds:0}s";
    public string FlightText=>$"{T(Flight.Simulator)}\n{T(Flight.Aircraft)}\n{Flight.AltitudeMSL:0} FT MSL · {Flight.AltitudeAGL:0} FT AGL\n{Flight.GroundSpeed:0} KT · {Flight.VerticalSpeed:0} FPM\n{T(Flight.OnGround?"On ground":"Airborne")} · AP {T(Flight.Autopilot?"ON":"OFF")}";
    public string TodText=>lastTod.NauticalMiles is {} nm?$"{nm:0.0} NM • {T(lastTod.Quality)}":T("TOD unavailable");
    public string TodExplanation=>T(lastTod.Explanation);
    public System.Windows.Media.Brush TodColor=>todIsClose?System.Windows.Media.Brushes.DarkOrange:System.Windows.Media.Brushes.Turquoise;
    public string Status { get; private set; }="";
    public string CaptureText=>T(CaptureCompatibility.Describe(Settings))+"\n"+T(Overlay.CaptureStatus);
    public string SimulatorNames=>exeXmlPath;
    public string SimConnectPathText=>SimConnectLibrary.LoadedPath??(string.IsNullOrWhiteSpace(dllPath)?T("Automatic installed-client detection"):dllPath);
    public string PlanSummary=>plan==null?"":$"{plan.Origin} → {plan.Destination} · {plan.Aircraft}\n{Choose("Planlanan rota","Planned route")} · {plan.Points.Count} {Choose("nokta","fixes")}{(plan.TodIndex<1?" · "+T("TOD unavailable"):"")}";
    public string SimBriefIdentity { get=>simBriefIdentity;set{simBriefIdentity=value;Notify(nameof(SimBriefIdentity));} }
    public bool HideSimBriefIdentity
    {
        get=>hideSimBriefIdentity;
        set
        {
            if(hideSimBriefIdentity==value)return;hideSimBriefIdentity=value;
            RefreshPrivacy();
            try{store.Save(store.Load() with{HideSimBriefIdentity=value});}catch(Exception e)when(e is IOException or UnauthorizedAccessException){store.Log(e);}
        }
    }
    public bool IsSimBriefMasked=>IdentityPrivacy.ShouldMask(Streaming,HideSimBriefIdentity);
    public string HideIdentityLabel=>T("Hide SimBrief identity");
    public string PrivacyHint=>T("Streaming mode always hides your Alias/Pilot ID.");
    private void RefreshPrivacy()
    {
        if(IsSimBriefMasked){operationMessage=IdentityPrivacy.Redact(operationMessage,SimBriefIdentity);Status=IdentityPrivacy.Redact(Status,SimBriefIdentity);}
        Notify(nameof(Streaming));Notify(nameof(HideSimBriefIdentity));Notify(nameof(IsSimBriefMasked));Notify(nameof(Status));
    }
    public bool SimBriefUsePilotId { get; set; }
    public string SelectedProfile
    {
        get=>selectedProfile;
        set{if(string.IsNullOrEmpty(value)||selectedProfile==value)return;selectedProfile=value;if(value!="Custom"){var p=value.StartsWith("VFR",StringComparison.Ordinal)?GuardianSettings.Vfr:new();LoadFields(p);TodAlerts=p.TodAlerts;}Notify("");}
    }
    public string[] Monitors=>monitorNames;
    public string Altitude { get; set; }="7000";
    public string Idle { get; set; }="60";
    public string Normal { get; set; }="540";
    public string Black { get; set; }="60";
    public string Countdown { get; set; }="10";
    public ProtectionMode Mode { get; set; }
    public AltitudeReference Reference { get; set; }
    public bool MiniTelemetry { get; set; }
    public int MonitorIndex { get=>monitorIndex;set{if(value>=0)monitorIndex=value;} }
    public bool Streaming { get=>streaming;set{if(streaming==value)return;streaming=value;RefreshPrivacy();} }
    public CaptureMethod CaptureMethod { get; set; }
    public bool ExcludeFromCapture { get; set; }
    public bool DisableUnverifiedCapture { get; set; }
    public bool CaptureVerified { get; set; }
    public bool AutoLaunch { get; set; }
    public bool TodAlerts { get; set; }
    public bool TodSound { get; set; }
    public bool TurkishVoice { get; set; }
    public double MockAltitude
    {
        get=>mockAltitude;
        set{if(!double.IsFinite(value)||value is <0 or >45000)throw new ArgumentOutOfRangeException(nameof(value));mockAltitude=value;Notify(nameof(MockAltitude));}
    }
    public double MockTod
    {
        get=>mockTod;
        set{if(!double.IsFinite(value)||value is <0 or >200)throw new ArgumentOutOfRangeException(nameof(value));mockTod=value;Notify(nameof(MockTod));Notify(nameof(MockTodText));}
    }
    public string MockTodText=>$"{MockTod:0.0} NM";
    public bool MockOnGround { get; set; }
    public bool MockPaused { get; set; }
    public bool MockDisconnected { get; set; }
    public ICommand SaveCommand { get; }
    public ICommand MockCommand { get; }
    public ICommand MsfsCommand { get; }
    public ICommand DemoCommand { get; }
    public ICommand ProtectCommand { get; }
    public ICommand WakeCommand { get; }
    public ICommand SkipCommand { get; }
    public ICommand SnoozeCommand { get; }
    public ICommand SuspendCommand { get; }
    public ICommand IdleCommand { get; }
    public ICommand ImportPlanCommand { get; }
    public ICommand FetchSimBriefCommand { get; }
    public ICommand SelectSimulatorCommand { get; }
    public ICommand SelectDllCommand { get; }
    public ICommand CheckUpdatesCommand { get; }
    public ICommand OpenReleaseCommand { get; }
    private Uri? releasePage;
    public string UpdateStatus { get; private set; }="";
    public string VersionText=>"FlightOLED Guardian v"+UpdateClient.CurrentVersion;
    public string CheckUpdatesLabel=>Choose("Güncellemeleri kontrol et","Check for updates");
    public string OpenReleaseLabel=>Choose("Yeni sürümü görüntüle","View new release");
    public bool UpdateAvailable=>releasePage!=null;
    public async Task CheckUpdatesAsync()
    {
        UpdateStatus=Choose("Güncellemeler kontrol ediliyor…","Checking for updates…");Notify(nameof(UpdateStatus));
        try
        {
            using var client=new UpdateClient();var result=await client.CheckAsync(lifetime.Token);
            if(disposed)return;releasePage=result.State==UpdateState.Available?result.ReleasePage:null;
            if(result.State==UpdateState.Available)Notice?.Invoke(Choose("Yeni Guardian sürümü: ","Guardian update available: ")+result.Version);
            UpdateStatus=result.State switch{UpdateState.Available=>Choose("Yeni sürüm: ","New version: ")+result.Version,UpdateState.Current=>Choose("Güncelsiniz.","You are up to date."),_=>Choose("Henüz yayımlanmış sürüm yok; güncellik doğrulanamadı.","No published release yet; update status cannot be verified.")};
        }
        catch(OperationCanceledException){if(disposed)return;UpdateStatus=Choose("Güncelleme kontrolü zaman aşımına uğradı.","Update check timed out.");}
        catch(Exception error)when(error is System.Net.Http.HttpRequestException or System.Text.Json.JsonException or IOException or InvalidOperationException or UriFormatException or KeyNotFoundException)
        {if(disposed)return;releasePage=null;UpdateStatus=Choose("Güncelleme kontrolü başarısız. Daha sonra tekrar deneyin.","Could not check for updates. Try again later.");}
        Notify(nameof(UpdateStatus));Notify(nameof(UpdateAvailable));
    }

    public GuardianViewModel(SettingsStore store,bool startMock,bool watcher)
    {
        this.store=store;this.watcher=watcher;Settings=store.Load();language=Settings.Language;UiText.Apply(language);RefreshChoiceLabels();
        Scheduler=new(Settings);reports=new(Path.Combine(store.DirectoryPath,"reports"));
        dllPath=Settings.SimConnectDllPath;exeXmlPath=Settings.MsfsExeXmlPath;
        if(string.IsNullOrWhiteSpace(exeXmlPath))exeXmlPath=MsfsAutoLaunchService.FindConfigurationFiles().FirstOrDefault()??"";
        provider=startMock?mock:new SimConnectProvider(dllPath);LoadFields(Settings);
        streaming=Settings.Streaming;hideSimBriefIdentity=Settings.HideSimBriefIdentity;CaptureMethod=Settings.CaptureMethod;ExcludeFromCapture=Settings.ExcludeFromCapture;DisableUnverifiedCapture=Settings.DisableUnverifiedCapture;CaptureVerified=Settings.CaptureVerified;
        AutoLaunch=Settings.AutoLaunch;TodAlerts=Settings.TodAlerts;TodSound=Settings.TodSound;TurkishVoice=Settings.TurkishVoice;
        simBriefIdentity=Settings.SimBriefIdentity;SimBriefUsePilotId=Settings.SimBriefUsePilotId;
        SaveCommand=new RelayCommand(Save);MockCommand=new RelayCommand(()=>Connect(true));MsfsCommand=new RelayCommand(()=>Connect(false));
        DemoCommand=new RelayCommand(()=>{Connect(true);accelerated=true;Settings=Settings with{Profile=GuardianSettings.Demo.Profile,IdleSeconds=3,NormalSeconds=10,BlackSeconds=5,CountdownSeconds=3,AltitudeConfirmationSeconds=0};Scheduler.Configure(Settings);LoadFields(Settings);SetMessage(Choose("Hızlandırılmış TEST açık; üretim varsayılanlarına kaydedilmez","Accelerated MOCK timings active; not saved as production defaults"));Notify("");});
        ProtectCommand=new RelayCommand(()=>{input.Changed();lastInput=clock.Elapsed.TotalSeconds;if(Flight.Valid && CaptureCompatibility.Allowed(Settings))Scheduler.ManualProtect(lastInput);else SetMessage(Choose("Telemetri ve yayın ayarlarını kontrol et","Check telemetry and streaming settings"));});
        WakeCommand=new RelayCommand(Wake);SkipCommand=new RelayCommand(()=>{input.Changed();Scheduler.Skip(clock.Elapsed.TotalSeconds);});
        SnoozeCommand=new RelayCommand(()=>Scheduler.Snooze(clock.Elapsed.TotalSeconds));SuspendCommand=new RelayCommand(()=>{if(hotkeyAvailable)Scheduler.Suspend(!Scheduler.Suspended);});
        IdleCommand=new RelayCommand(()=>{if(provider==mock && accelerated){input.Changed();lastInput=clock.Elapsed.TotalSeconds-Settings.IdleSeconds;}else SetMessage(Choose("Bu seçenek yalnızca hızlandırılmış testte kullanılabilir","Simulated idle requires accelerated MOCK mode"));});
        ImportPlanCommand=new RelayCommand(ImportPlan);FetchSimBriefCommand=new AsyncRelayCommand(FetchSimBriefAsync);SelectSimulatorCommand=new RelayCommand(SelectSimulator);SelectDllCommand=new RelayCommand(SelectDll);
        CheckUpdatesCommand=new AsyncRelayCommand(CheckUpdatesAsync);
        OpenReleaseCommand=new RelayCommand(()=>{if(releasePage!=null)Process.Start(new ProcessStartInfo(releasePage.AbsoluteUri){UseShellExecute=true});});
        Overlay.Skip+=()=>Scheduler.Skip(clock.Elapsed.TotalSeconds);Overlay.Snooze+=()=>Scheduler.Snooze(clock.Elapsed.TotalSeconds);Overlay.Wake+=Wake;
        RefreshHistory();
    }
    private void LoadFields(GuardianSettings s)
    {
        Altitude=s.ActivationFeet.ToString(CultureInfo.InvariantCulture);Idle=s.IdleSeconds.ToString(CultureInfo.InvariantCulture);Normal=s.NormalSeconds.ToString(CultureInfo.InvariantCulture);Black=s.BlackSeconds.ToString(CultureInfo.InvariantCulture);Countdown=s.CountdownSeconds.ToString(CultureInfo.InvariantCulture);Mode=s.Mode;Reference=s.Reference;MiniTelemetry=s.MiniTelemetry;MonitorIndex=s.MonitorIndex;selectedProfile=s.Profile;
    }
    private void RefreshChoiceLabels()
    {
        foreach(var choice in profileChoices.Concat(modeChoices).Concat(captureChoices))choice.Rename(T(choice.Value.ToString()!));
        if(monitorNames.Length==0)monitorNames=System.Windows.Forms.Screen.AllScreens.Select((s,i)=>$"{i+1} • {s.DeviceName} • {s.Bounds.Width}×{s.Bounds.Height}{(s.Primary?" ★":"")}").ToArray();
    }
    private void Connect(bool useMock)
    {
        Wake();if(provider!=mock)provider.Dispose();provider=useMock?mock:new SimConnectProvider(dllPath);recorder=new();plan=null;todAlert.Reset();previousAircraft=null;
        if(accelerated){Settings=store.Load() with{Language=UiLanguage};LoadFields(Settings);Scheduler.Configure(Settings);accelerated=false;}
        SetMessage(T(useMock?"MOCK mode enabled":"Connecting to MSFS"));Notify("");
    }
    public void Tick()
    {
        double now=clock.Elapsed.TotalSeconds;
        try
        {
            if(input.Changed()){lastInput=now;Scheduler.Wake();Overlay.Hide();}
            if(watcher)
            {
                if(now-watchCheckedAt>=5){watchedSimulatorRunning=AutoLaunchService.SimulatorRunning(Settings.SimulatorProcesses);watchCheckedAt=now;}
                if(!watchedSimulatorRunning){Scheduler.Wake();Overlay.Hide();Status=Choose("Eski başlangıç izleyicisi • ayarlardan EXE.xml yöntemine geç","Legacy watcher • switch to EXE.xml in Settings");Notify(nameof(Status));return;}
            }
            if(provider==mock)mock.Sample=mock.Sample with{AltitudeMSL=MockAltitude,AltitudeAGL=Math.Max(0,MockAltitude-500),OnGround=MockOnGround,Paused=MockPaused,Connected=!MockDisconnected,GroundSpeed=MockOnGround?40:450,VerticalSpeed=MockOnGround?0:-150};
            Flight=provider.Poll(now);
            if(Flight.Connected && (previousAircraft!=Flight.Aircraft || previousSession!=Flight.SessionId))
            {
                if(previousAircraft!=null){Scheduler.Wake();recorder=new();plan=null;todAlert.Reset();lastInput=now;Notify(nameof(PlanSummary));}
                previousAircraft=Flight.Aircraft;previousSession=Flight.SessionId;
            }
            Scheduler.Tick(now,Math.Max(0,now-lastInput),Flight,CaptureCompatibility.Allowed(Settings));
            var reading=provider==mock?new TodReading(MockTod,"MOCK / simulated TOD","mock","Simulated test data; not a real FMS TOD"):
                plan!=null && Flight.Valid && now-Flight.ReceivedAt<5?TodRadar.AlongRoute(new(Flight.Latitude,Flight.Longitude),plan.Points,plan.TodIndex,plan.Id):new(null,"Unavailable","none","No verified FMS or planned TOD data");
            lastTod=reading;todIsClose=reading.NauticalMiles is >=0 and <=50;
            if(Settings.TodAlerts && Flight.Valid && !Flight.OnGround && todAlert.Evaluate(reading))
            {
                Scheduler.Snooze(now,30);Overlay.Hide();var message=$"{reading.NauticalMiles:0.0} NM • {T(reading.Quality)}";Notice?.Invoke(T("TOP OF DESCENT APPROACHING")+": "+message);Overlay.ShowTod(message,Settings);if(Settings.TodSound)Speak();
            }
            Overlay.Update(Scheduler.State,Scheduler.RemainingSeconds,Flight,Settings,now,TodText,todIsClose);
            if(Settings.Streaming && Settings.ExcludeFromCapture && Settings.DisableUnverifiedCapture && Scheduler.State is ProtectionState.Black or ProtectionState.Countdown && !Overlay.CaptureExclusionAccepted){Overlay.Hide();Scheduler.Suspend(true);}
            var report=recorder.Update(now,DateTimeOffset.UtcNow,Flight,Scheduler);
            if(report!=null){reports.Save(report);LatestReport=report;RefreshHistory();Notice?.Invoke(Choose("Uçuş tamamlandı • raporun hazır","Flight completed • your report is ready"));}
            string protection=!hotkeyAvailable?T("Emergency hotkey unavailable"):Scheduler.Suspended?T("Protection suspended"):EmergencyShortcut;
            Status=fetching?T("Fetching flight plan…"):now<messageUntil?UiText.Relocalize(operationMessage,UiLanguage):$"{T(provider.Status)} · {(accelerated?T("MOCK accelerated test")+" · ":"")}{protection}";
            foreach(var name in new[]{nameof(StateText),nameof(ProtectionReason),nameof(TimerText),nameof(EligibilityText),nameof(FlightText),nameof(TodText),nameof(TodColor),nameof(TodExplanation),nameof(Status),nameof(CaptureText),nameof(SimConnectPathText)})Notify(name);
        }
        catch(Exception e){Overlay.Hide();Scheduler.Suspend(true);store.Log(e);SetMessage(T("Protection safely suspended")+": "+e.Message);}
    }
    private void Speak()
    {
        try
        {
            var type=Type.GetTypeFromProgID("SAPI.SpVoice");if(type==null)return;speechVoice??=Activator.CreateInstance(type);if(speechVoice==null)return;
            dynamic voice=speechVoice;bool turkish=false;
            dynamic voices=voice.GetVoices(TurkishVoice?"Language=41f":"Language=409","");
            try{if(voices.Count>0){voice.Voice=voices.Item(0);turkish=TurkishVoice;}}finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(voices);}
            voice.Speak(turkish?"Dikkat. Alçalış başlangıç noktasına elli deniz mili kaldı.":"Attention. Top of descent in fifty nautical miles.",1);
        }
        catch(Exception e){store.Log(e);}
    }
    private void Save()
    {
        try
        {
            if(accelerated)throw new ArgumentException(Choose("Ayar kaydetmeden önce hızlandırılmış testten çık","Exit accelerated MOCK mode before saving production settings"));
            double Parse(string value)=>double.Parse(value,CultureInfo.InvariantCulture);
            var value=Settings with{Language=UiLanguage,HideSimBriefIdentity=HideSimBriefIdentity,SimBriefIdentity=SimBriefIdentity.Trim(),SimBriefUsePilotId=SimBriefUsePilotId,SimConnectDllPath=dllPath,MsfsExeXmlPath=exeXmlPath,ActivationFeet=Parse(Altitude),IdleSeconds=Parse(Idle),NormalSeconds=Parse(Normal),BlackSeconds=Parse(Black),CountdownSeconds=Parse(Countdown),Mode=Mode,Reference=Reference,Profile=SelectedProfile,SuppressManeuvers=SelectedProfile.StartsWith("VFR",StringComparison.Ordinal),MiniTelemetry=MiniTelemetry,MonitorIndex=MonitorIndex,Streaming=Streaming,CaptureMethod=CaptureMethod,ExcludeFromCapture=ExcludeFromCapture,DisableUnverifiedCapture=DisableUnverifiedCapture,CaptureVerified=CaptureVerified,TodAlerts=TodAlerts,TodSound=TodSound,TurkishVoice=TurkishVoice,AutoLaunch=AutoLaunch,OnboardingComplete=true};
            value.Validate();
            if(value.AutoLaunch || value.AutoLaunch!=Settings.AutoLaunch)
            {
                if(string.IsNullOrWhiteSpace(exeXmlPath) && value.AutoLaunch)throw new ArgumentException(Choose("Önce MSFS EXE.xml dosyasını seç","Select MSFS EXE.xml first"));
                if(!string.IsNullOrWhiteSpace(exeXmlPath))MsfsAutoLaunchService.Configure(exeXmlPath,Path.Combine(AppContext.BaseDirectory,"FlightOLEDGuardian.App.exe"),value.AutoLaunch);
                if(Settings.AutoLaunch && !string.IsNullOrWhiteSpace(Settings.MsfsExeXmlPath) && Settings.MsfsExeXmlPath!=exeXmlPath)MsfsAutoLaunchService.Configure(Settings.MsfsExeXmlPath,Path.Combine(AppContext.BaseDirectory,"FlightOLEDGuardian.App.exe"),false);
                if(Settings.AutoLaunch)AutoLaunchService.SetEnabled(false); // Remove the v0.1 watcher only after explicit settings save.
            }
            if(value.CaptureMethod!=Settings.CaptureMethod || value.MonitorIndex!=Settings.MonitorIndex)value=value with{CaptureVerified=false};
            store.Save(value);Settings=value;Scheduler.Configure(value);Wake();SetMessage(T("Settings saved"));Notify("");
        }
        catch(Exception e)when(e is ArgumentException or FormatException or OverflowException or IOException or UnauthorizedAccessException or System.Xml.XmlException or InvalidOperationException or System.Security.SecurityException){SetMessage(T("Settings not saved")+": "+e.Message);}
    }
    public void CompleteOnboarding(bool automatic){AutoLaunch=automatic;if(automatic && string.IsNullOrWhiteSpace(exeXmlPath))SelectSimulator();if(automatic && string.IsNullOrWhiteSpace(exeXmlPath))AutoLaunch=false;Save();}
    public void CompleteLanguageSelection(string selected)
    {
        UiLanguage=selected;Settings=Settings with{Language=selected,LanguageSelectionCompleted=true};
        store.Save(store.Load() with{Language=selected,LanguageSelectionCompleted=true});
    }
    private void SelectSimulator()
    {
        var dialog=new Microsoft.Win32.OpenFileDialog{Title=T("Select MSFS EXE.xml"),Filter="MSFS EXE.xml|exe.xml|XML|*.xml",FileName="exe.xml",InitialDirectory=Path.GetDirectoryName(exeXmlPath)};
        if(dialog.ShowDialog()==true){exeXmlPath=dialog.FileName;Notify(nameof(SimulatorNames));}
    }
    private void SelectDll()
    {
        var dialog=new Microsoft.Win32.OpenFileDialog{Title=T("Find / select SimConnect.dll"),Filter="Native SimConnect client|SimConnect.dll",FileName="SimConnect.dll",InitialDirectory=Path.GetDirectoryName(SimConnectLibrary.FindCandidates().FirstOrDefault())};
        if(dialog.ShowDialog()!=true)return;
        try{if(!SimConnectLibrary.IsX64(dialog.FileName))throw new ArgumentException(Choose("x64 native SimConnect.dll gerekli","An x64 native SimConnect.dll is required"));dllPath=dialog.FileName;Notify(nameof(SimConnectPathText));SetMessage(Choose("DLL seçildi. Ayarları kaydet; farklı bir DLL yüklemek için Guardian'ı yeniden başlat.","DLL selected. Save settings; restart Guardian to load a different DLL."));}
        catch(Exception e)when(e is IOException or ArgumentException){SetMessage(e.Message);}
    }
    private async Task FetchSimBriefAsync()
    {
        fetching=true;Notify(nameof(Status));
        try
        {
            var imported=await simbrief.FetchAsync(SimBriefIdentity,SimBriefUsePilotId,lifetime.Token);
            if(disposed)return;plan=imported;
            store.Save(store.Load() with{SimBriefIdentity=SimBriefIdentity.Trim(),SimBriefUsePilotId=SimBriefUsePilotId,Language=UiLanguage});
            SetMessage(Choose("SimBrief planı alındı: ","SimBrief plan imported: ")+plan.Origin+" → "+plan.Destination);Notify(nameof(PlanSummary));
        }
        catch(OperationCanceledException){if(!disposed)SetMessage(Choose("SimBrief isteği iptal edildi veya zaman aşımına uğradı","SimBrief request cancelled or timed out"));}
        catch(Exception e){if(!disposed){store.Log(new InvalidOperationException(IdentityPrivacy.Redact(e.Message,SimBriefIdentity)));SetMessage(Choose("SimBrief alınamadı: ","SimBrief import failed: ")+T(e.Message));}}
        finally{fetching=false;if(!disposed)Notify(nameof(Status));}
    }
    private void ImportPlan()
    {
        var dialog=new Microsoft.Win32.OpenFileDialog{Title=T("Import SimBrief XML"),Filter="XML|*.xml"};if(dialog.ShowDialog()!=true)return;
        try{plan=FlightPlanImport.ReadSimBrief(dialog.FileName);SetMessage(Choose("Planlanan TOD içe aktarıldı; FMS değeri farklı olabilir","Planned TOD imported; aircraft FMS TOD may differ"));}catch(Exception e)when(e is IOException or System.Xml.XmlException or FormatException){plan=null;SetMessage(e.Message);}Notify(nameof(PlanSummary));
    }
    public string ReportText=>LatestReport is not {} r?T("Your report appears after an observed takeoff and a confirmed landing. No completed flight recorded yet."):
        $"{T("FLIGHT COMPLETED")}{(r.IsMock?" • "+T("MOCK / TEST DATA"):"")}\n\n{r.Aircraft}\n{r.TouchdownUtc.ToLocalTime():dd MMM yyyy HH:mm}\n\n{T("FLIGHT TIME")}   {TimeSpan.FromSeconds(r.FlightSeconds):hh\\:mm\\:ss}\n{T("OLED BLACK TIME")}   {TimeSpan.FromSeconds(r.BlackSeconds):hh\\:mm\\:ss}\n{T("ELIGIBLE TIME")}   {TimeSpan.FromSeconds(r.EligibleSeconds):hh\\:mm\\:ss}\n\n{r.Cycles} {Choose("döngü","cycles")} · {r.Skips} {Choose("atlama","skips")} · {r.Wakes} {Choose("uyanma","wakes")}\n{T("Longest black interval")}   {r.LongestBlackSeconds:0} s\n\n{T("Landing rate (sampled)")}: {r.LandingRate:0} FPM\n{T("Ground speed at touchdown")}: {r.TouchdownGroundSpeed:0} KT";
    public string HistoryText { get; private set; }="";
    private void RefreshHistory()
    {
        try
        {
            var history=reports.Load();LatestReport??=history.FirstOrDefault();var real=history.Where(r=>!r.IsMock).ToArray();
            HistoryText=$"{T("REAL FLIGHT HISTORY")}\n{real.Length} {Choose("tamamlanan uçuş","completed flights")} · {TimeSpan.FromSeconds(real.Sum(r=>r.FlightSeconds)):g}\n{T("OLED BLACK TIME")}: {TimeSpan.FromSeconds(real.Sum(r=>r.BlackSeconds)):g} · {real.Sum(r=>r.Cycles)} {Choose("döngü","cycles")}\n\n{T("ACHIEVEMENTS")}\n{(real.Any(r=>r.BlackSeconds>0)?"✓":"○")} First Guardian Flight\n{(real.Sum(r=>r.BlackSeconds)>=3600?"✓":"○")} OLED Sentinel\n{(real.Sum(r=>r.Cycles)>=100?"✓":"○")} 100 Cycles Club\n\n{Choose("Test raporları (gerçek toplamlara dahil değil)","Mock reports (excluded from real totals)")}: {history.Count(r=>r.IsMock)}";
            Notify(nameof(ReportText));Notify(nameof(HistoryText));
        }
        catch(Exception e)when(e is IOException or UnauthorizedAccessException){store.Log(e);SetMessage(e.Message);}
    }
    private void SetMessage(string text){if(IsSimBriefMasked)text=IdentityPrivacy.Redact(text,SimBriefIdentity);operationMessage=text;messageUntil=clock.Elapsed.TotalSeconds+20;Status=text;Notify(nameof(Status));}
    public void Wake(){lastInput=clock.Elapsed.TotalSeconds;Scheduler.Wake();Overlay.Hide();}
    public void Emergency(){Wake();Scheduler.Suspend(true);Notice?.Invoke(T("Protection suspended"));}
    public void SetHotkeyStatus(bool available,string shortcut="Ctrl + Alt + F12"){hotkeyAvailable=available;EmergencyShortcut=shortcut;if(!available)Scheduler.Suspend(true);Notify(nameof(EmergencyHelp));}
    public void DisplayChanged(){Wake();Scheduler.Suspend(true);monitorNames=[];RefreshChoiceLabels();Notify(nameof(Monitors));System.Windows.Application.Current?.Dispatcher.BeginInvoke(()=>Notify(nameof(MonitorIndex)));}
    private void Notify(string name)=>PropertyChanged?.Invoke(this,new(name));
    public void Dispose(){disposed=true;lifetime.Cancel();Overlay.Dispose();provider.Dispose();simbrief.Dispose();lifetime.Dispose();if(speechVoice!=null){System.Runtime.InteropServices.Marshal.FinalReleaseComObject(speechVoice);speechVoice=null;}}
}
