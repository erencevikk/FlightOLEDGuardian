using FlightOLEDGuardian.Core;
using FlightOLEDGuardian.Windows;
using FlightOLEDGuardian.Integrations;
using FlightOLEDGuardian.Reporting;
using FlightOLEDGuardian.SimConnect;
using System.IO;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if(args.Contains("--live"))
        {
            using var live=new SimConnectProvider();var clock=System.Diagnostics.Stopwatch.StartNew();string previous="";
            while(clock.Elapsed.TotalSeconds<20)
            {
                var sample=live.Poll(clock.Elapsed.TotalSeconds);
                if(live.Status!=previous){Console.WriteLine(live.Status);previous=live.Status;}
                if(sample.Connected){Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(sample));Console.WriteLine("DLL: "+SimConnectLibrary.LoadedPath);return 0;}
                System.Threading.Thread.Sleep(100);
            }
            Console.WriteLine("Live simulator connection not established. DLL: "+SimConnectLibrary.LoadedPath);return 2;
        }
        int passed=0,failed=0;
        string root=Path.Combine(Environment.CurrentDirectory,"work","integration",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        void Check(string name,Action test){try{test();Console.WriteLine("PASS "+name);passed++;}catch(Exception e){Console.WriteLine("FAIL "+name+": "+e);failed++;}}
        void Assert(bool value,string message){if(!value)throw new Exception(message);}
        Check("Update check compares numeric versions and uses only the expected GitHub feed",()=>
        {
            var handler=new StubHttpHandler(request=>{Assert(request.RequestUri!.AbsoluteUri=="https://api.github.com/repos/"+UpdateClient.Repository+"/releases/latest","Unexpected feed");Assert(request.Headers.UserAgent.ToString().Contains("FlightOLEDGuardian"),"User agent missing");return new(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent("{\"tag_name\":\"v0.10.0\",\"html_url\":\"https://github.com/"+UpdateClient.Repository+"/releases/tag/v0.10.0\"}")};});
            using var http=new System.Net.Http.HttpClient(handler);using var updater=new UpdateClient(http);var result=updater.CheckAsync().GetAwaiter().GetResult();Assert(result.State==UpdateState.Available,"Numeric version comparison failed");Assert(handler.Requests==1,"Unexpected polling");
        });
        Check("Missing release is not reported as up to date",()=>{using var http=new System.Net.Http.HttpClient(new StubHttpHandler(_=>new(System.Net.HttpStatusCode.NotFound)));using var updater=new UpdateClient(http);Assert(updater.CheckAsync().GetAwaiter().GetResult().State==UpdateState.NoRelease,"Missing repository reported current");});
        Check("Current release and preview releases are distinguished",()=>
        {
            foreach(bool preview in new[]{false,true}){using var http=new System.Net.Http.HttpClient(new StubHttpHandler(_=>new(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent("{\"tag_name\":\"v0.3.0\",\"prerelease\":"+preview.ToString().ToLowerInvariant()+",\"html_url\":\"https://github.com/"+UpdateClient.Repository+"/releases/tag/v0.3.0\"}")}));using var updater=new UpdateClient(http);Assert(updater.CheckAsync().GetAwaiter().GetResult().State==(preview?UpdateState.NoRelease:UpdateState.Current),"Release channel confused");}
        });
        Check("Updates reject unrelated download pages and oversized responses",()=>
        {
            foreach(string response in new[]{"{\"tag_name\":\"v9.0.0\",\"html_url\":\"https://evil.example/update.exe\"}",new string('x',262145)}){using var http=new System.Net.Http.HttpClient(new StubHttpHandler(_=>new(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent(response)}));using var updater=new UpdateClient(http);try{updater.CheckAsync().GetAwaiter().GetResult();throw new Exception("Untrusted response accepted");}catch(InvalidDataException){}}
        });
        Check("Update cancellation propagates without changing flight protection",()=>{using var http=new System.Net.Http.HttpClient(new StubHttpHandler(_=>new(System.Net.HttpStatusCode.OK)));using var updater=new UpdateClient(http);using var cancelled=new CancellationTokenSource();cancelled.Cancel();try{updater.CheckAsync(cancelled.Token).GetAwaiter().GetResult();throw new Exception("Cancellation ignored");}catch(OperationCanceledException){}});
        Check("Settings atomic roundtrip",()=>{var store=new SettingsStore(root);var s=GuardianSettings.Vfr with{OnboardingComplete=true};store.Save(s);Assert(store.Load().ActivationFeet==3000,"Altitude lost");Assert(store.Load().Reference==AltitudeReference.AGL,"AGL lost");});
        Check("Old standard IFR defaults migrate to AGL without replacing language",()=>
        {
            var folder=Path.Combine(root,"legacy-ifr");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"settings.json"),"{\"Language\":\"tr\",\"Reference\":0,\"Profile\":\"IFR / Airliner\",\"ActivationFeet\":7000,\"OnboardingComplete\":true}");
            var s=new SettingsStore(folder).Load();Assert(s.Reference==AltitudeReference.AGL && s.DefaultsRevision==3,"AGL migration failed");Assert(s.Language=="tr" && !s.LanguageSelectionCompleted,"Language choice incorrectly replaced/skipped");
        });
        Check("Explicit custom MSL remains selectable after migration",()=>
        {
            var folder=Path.Combine(root,"legacy-custom");Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"settings.json"),"{\"Reference\":0,\"Profile\":\"Custom\",\"ActivationFeet\":9000}");Assert(new SettingsStore(folder).Load().Reference==AltitudeReference.MSL,"Custom reference overwritten");
        });
        Check("Privacy and first-language completion persist",()=>{var store=new SettingsStore(Path.Combine(root,"privacy"));store.Save(new(){Streaming=true,HideSimBriefIdentity=false,Language="en",LanguageSelectionCompleted=true});var s=store.Load();Assert(IdentityPrivacy.ShouldMask(s.Streaming,s.HideSimBriefIdentity) && s.LanguageSelectionCompleted,"Privacy/language preference lost");});
        Check("Malformed settings recover without overwriting",()=>{var file=Path.Combine(root,"settings.json");File.WriteAllText(file,"{broken");var store=new SettingsStore(root);Assert(store.Load().ActivationFeet==7000,"Defaults missing");Assert(store.Warning!=null,"No warning");Assert(File.ReadAllText(file)=="{broken","Damaged file overwritten");});
        Check("Capture card / unverified streaming fail open",()=>{Assert(!CaptureCompatibility.Allowed(new(){Streaming=true}),"Unverified capture enabled");Assert(CaptureCompatibility.Allowed(new(){Streaming=true,CaptureVerified=true}),"Verified game capture denied");Assert(!CaptureCompatibility.Allowed(new(){Streaming=true,CaptureVerified=true,CaptureMethod=CaptureMethod.CaptureCard}),"Physical card marked safe");});
        Check("Watcher startup command quotes apphost path",()=>{Assert(AutoLaunchService.BuildCommand(@"C:\Test Folder\FlightOLEDGuardian.App.exe")=="\"C:\\Test Folder\\FlightOLEDGuardian.App.exe\" --watch","Incorrect startup command");});
        Check("Single-instance named mutex rejects a second owner",()=>{var name=@"Local\FlightOLEDGuardian.Test."+Guid.NewGuid().ToString("N");using var first=new System.Threading.Mutex(true,name,out bool created);using var second=new System.Threading.Mutex(true,name,out bool duplicateCreated);Assert(created && !duplicateCreated,"Duplicate instance admitted");first.ReleaseMutex();});
        Check("SimBrief XML navlog TOD import",()=>{var path=Path.Combine(root,"route.xml");File.WriteAllText(path,"<OFP><navlog><fix><ident>DEP</ident><pos_lat>0</pos_lat><pos_long>0</pos_long></fix><fix><ident>TOD</ident><pos_lat>0</pos_lat><pos_long>1</pos_long></fix></navlog></OFP>");var plan=FlightPlanImport.ReadSimBrief(path);Assert(plan.TodIndex==1,"TOD missing");Assert(TodRadar.AlongRoute(new(0,.5),plan.Points,plan.TodIndex,plan.Id).NauticalMiles is >29 and <31,"Distance wrong");});
        Check("SimBrief alias is encoded and Pilot ID validated",()=>
        {
            var uri=SimBriefClient.BuildUri("test&userid=other",false);Assert(uri.Query.Contains("username=test%26userid%3Dother",StringComparison.OrdinalIgnoreCase),"Query injection");Assert(uri.Query.Contains("client=FlightOLEDGuardian-0.2"),"Client identification missing");
            Assert(SimBriefClient.BuildUri("1234567",true).Query.Contains("userid=1234567"),"Pilot ID parameter missing");
            foreach(var invalid in new[]{"","abc","12345678","123&x=1"}){try{SimBriefClient.BuildUri(invalid,true);throw new Exception("Invalid ID accepted");}catch(ArgumentException){}}
        });
        Check("SimBrief HTTP import, stable plan ID and missing TOD",()=>
        {
            const string xml="<OFP><origin><icao_code>LTBA</icao_code></origin><destination><icao_code>LTAI</icao_code></destination><aircraft><icaocode>A320</icaocode></aircraft><navlog><fix><ident>DEP</ident><pos_lat>0</pos_lat><pos_long>0</pos_long></fix><fix><ident>TOD</ident><pos_lat>0</pos_lat><pos_long>1</pos_long></fix></navlog></OFP>";
            var handler=new StubHttpHandler(_=>new(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent(xml)});using var http=new System.Net.Http.HttpClient(handler);using var client=new SimBriefClient(http);
            var route=client.FetchAsync("test-alias",false).GetAwaiter().GetResult();Assert(route.Origin=="LTBA" && route.Destination=="LTAI" && route.TodIndex==1,"Flight plan fields missing");
            Assert(route.Id==client.FetchAsync("test-alias",false).GetAwaiter().GetResult().Id,"Same OFP resets TOD alert identity");Assert(handler.Requests==2,"Unexpected background polling");
            var noTod=FlightPlanImport.FromXml(xml.Replace("<ident>TOD</ident>","<ident>ARR</ident>"));Assert(noTod.TodIndex==-1,"TOD fabricated");
        });
        Check("SimBrief HTTP errors surface without inventing a flight",()=>
        {
            using var http=new System.Net.Http.HttpClient(new StubHttpHandler(_=>new(System.Net.HttpStatusCode.BadRequest)));using var client=new SimBriefClient(http);
            try{client.FetchAsync("unknown",false).GetAwaiter().GetResult();throw new Exception("Bad user accepted");}catch(InvalidOperationException e){Assert(e.Message.Contains("not found"),"Unhelpful error");}
        });
        Check("SimBrief oversized response and cancellation are bounded",()=>
        {
            using var http=new System.Net.Http.HttpClient(new StubHttpHandler(_=>{var response=new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new System.Net.Http.StringContent("")};response.Content.Headers.ContentLength=2_000_001;return response;}));using var client=new SimBriefClient(http);
            try{client.FetchAsync("test",false).GetAwaiter().GetResult();throw new Exception("Oversized OFP accepted");}catch(InvalidDataException){}
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();try{client.FetchAsync("test",false,cancelled.Token).GetAwaiter().GetResult();throw new Exception("Cancellation ignored");}catch(OperationCanceledException){}
        });
        Check("Language preference persists and labels differ",()=>
        {
            var store=new SettingsStore(Path.Combine(root,"language"));store.Save(new(){Language="en",SimBriefIdentity="test-pilot",SimBriefUsePilotId=true});Assert(store.Load().Language=="en" && store.Load().SimBriefIdentity=="test-pilot","Preferences lost");
            Assert(UiText.Get("Settings","tr")=="Ayarlar" && UiText.Get("Settings","en")=="Settings","Translation incorrect");Assert(UiText.Get("PeriodicBlack","tr")=="Periyodik siyah ekran","Enum not localized");
        });
        Check("Every main-window text resource has a translation",()=>
        {
            var file=Path.Combine(Environment.CurrentDirectory,"src","FlightOLEDGuardian.App","MainWindow.xaml");
            var keys=UiText.Translations.Keys.Select(UiText.ResourceKey).ToHashSet();
            foreach(System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(file),@"\{DynamicResource (Ui_[A-F0-9]+)\}"))Assert(keys.Contains(match.Groups[1].Value),"Unresolved text resource: "+match.Groups[1].Value);
        });
        Check("Native resolver rejects non-PE files",()=>{var fake=Path.Combine(root,"not-a-dll.dll");File.WriteAllText(fake,"invalid DLL");Assert(!SimConnectLibrary.IsX64(fake),"Invalid binary accepted");});
        Check("XML DTD blocked",()=>{var path=Path.Combine(root,"unsafe.xml");File.WriteAllText(path,"<!DOCTYPE OFP [<!ENTITY e SYSTEM 'file:///nonexistent'>]><OFP>&e;</OFP>");try{FlightPlanImport.ReadSimBrief(path);throw new Exception("DTD accepted");}catch(System.Xml.XmlException){}});
        Check("Missing TOD never fabricated",()=>{var path=Path.Combine(root,"no-tod.xml");File.WriteAllText(path,"<OFP><navlog /></OFP>");try{FlightPlanImport.ReadSimBrief(path);throw new Exception("TOD invented");}catch(FormatException){}});
        Check("Report persists and corrupt neighbor is ignored",()=>{var store=new ReportStore(root);var report=new FlightReport(Guid.NewGuid().ToString("N"),"TEST",true,DateTimeOffset.UtcNow,100,50,10,5,2,1,1,-150,70);store.Save(report);File.WriteAllText(Path.Combine(root,"flight-corrupt.json"),"bad");Assert(store.Load().Count==1,"Report reload failed");Assert(store.Load()[0].BlackSeconds==10,"Wrong black duration");});
        Check("SimConnect unavailable remains disconnected",()=>{using var provider=new SimConnectProvider(Path.Combine(root,"missing","SimConnect.dll"),false);var f=provider.Poll(0);Assert(!f.Connected,"Unexpected live connection");Assert(provider.Status.Contains("SimConnect",StringComparison.OrdinalIgnoreCase),"Missing diagnostic");});
        Check("MSFS EXE.xml preserves other addons and deduplicates Guardian",()=>
        {
            var xml=Path.Combine(root,"exe.xml");var exe=Path.Combine(root,"Guardian.exe");File.WriteAllText(exe,"test placeholder, never executed");
            File.WriteAllText(xml,"<SimBase.Document Type=\"Launch\" version=\"1,0\"><Disabled>False</Disabled><Launch.Addon><Name>Other app</Name><Path>C:\\Other.exe</Path></Launch.Addon></SimBase.Document>");
            MsfsAutoLaunchService.Configure(xml,exe,true);MsfsAutoLaunchService.Configure(xml,exe,true);
            var doc=System.Xml.Linq.XDocument.Load(xml);Assert(doc.Descendants("Launch.Addon").Count()==2,"Duplicate or lost addon");Assert(doc.Descendants("CommandLine").Single().Value=="--tray","Tray launch missing");
            MsfsAutoLaunchService.Configure(xml,exe,false);doc=System.Xml.Linq.XDocument.Load(xml);Assert(doc.Descendants("Launch.Addon").Count()==1 && doc.Descendants("Name").Single().Value=="Other app","Removal damaged other addon");Assert(Directory.GetFiles(root,"exe.xml.flightoled-*.bak").Length==3,"Backups missing");
        });
        Check("Malformed EXE.xml is never overwritten",()=>{var xml=Path.Combine(root,"exe.xml");File.WriteAllText(xml,"<broken>");try{MsfsAutoLaunchService.Configure(xml,Path.Combine(root,"Guardian.exe"),true);throw new Exception("Malformed file accepted");}catch(System.Xml.XmlException){}Assert(File.ReadAllText(xml)=="<broken>","Original overwritten");});
        Check("Globally disabled MSFS startup is preserved",()=>{var xml=Path.Combine(root,"exe.xml");const string disabled="<SimBase.Document><Disabled>True</Disabled></SimBase.Document>";File.WriteAllText(xml,disabled);try{MsfsAutoLaunchService.Configure(xml,Path.Combine(root,"Guardian.exe"),true);throw new Exception("Global disable bypassed");}catch(InvalidOperationException){}Assert(File.ReadAllText(xml)==disabled,"Global settings changed");});
        Check("SimConnect binary telemetry layout and rejection",()=>
        {
            using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
            foreach(uint field in new uint[]{368,0,8,1,0,1,0,1,1,10})writer.Write(field);
            foreach(double value in new[]{35000d,32000,450,-150,0,1,40,28,0})writer.Write(value);
            byte[] title=new byte[256];System.Text.Encoding.ASCII.GetBytes("Test A320").CopyTo(title,0);writer.Write(title);
            byte[] packet=stream.ToArray();var flight=TelemetryPacketReader.Decode(packet,12,true,false,"test");
            Assert(flight is {AltitudeMSL:35000,AltitudeAGL:32000,GroundSpeed:450,VerticalSpeed:-150,Aircraft:"Test A320",OnGround:false,Autopilot:true},"Native data layout mismatch");
            Assert(TelemetryPacketReader.Decode(packet.AsSpan(0,100),12,true,false,"test")==null,"Truncated packet accepted");
            packet[36]=9;Assert(TelemetryPacketReader.Decode(packet,12,true,false,"test")==null,"Wrong definition count accepted");
        });
        Check("PNG report exports at all three exact dimensions",()=>
        {
            var report=new FlightReport("test","Mock Airbus A320",true,DateTimeOffset.UnixEpoch,4320,3600,360,60,6,1,2,-145,125);
            foreach(var size in new[]{(1920,1080),(1080,1920),(1080,1080)})
            {
                string path=Path.Combine(root,$"report-{size.Item1}x{size.Item2}.png");ReportCardRenderer.Export(report,size.Item1,size.Item2,path);
                using var stream=File.OpenRead(path);var decoder=new System.Windows.Media.Imaging.PngBitmapDecoder(stream,System.Windows.Media.Imaging.BitmapCreateOptions.None,System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                Assert(decoder.Frames[0].PixelWidth==size.Item1 && decoder.Frames[0].PixelHeight==size.Item2,"Wrong image dimensions");
            }
            Console.WriteLine("Report previews: "+root);
        });
        if(args.Contains("--wpf"))
        {
            var app=new System.Windows.Application{ShutdownMode=System.Windows.ShutdownMode.OnExplicitShutdown};
            Check("WPF window create/render/close",()=>{var window=new System.Windows.Window{Width=400,Height=300,Content=new System.Windows.Controls.TextBlock{Text="FlightOLED Guardian integration test"}};window.Show();window.UpdateLayout();var bmp=new System.Windows.Media.Imaging.RenderTargetBitmap(400,300,96,96,System.Windows.Media.PixelFormats.Pbgra32);bmp.Render(window);window.Close();});
            Check("Black overlay is pure black and hides completely",()=>
            {
                using var overlay=new OverlayManager();
                overlay.Update(ProtectionState.Black,60,new(){Connected=true},new(),0);
                var window=overlay.ActiveWindow??throw new Exception("Overlay not created");window.UpdateLayout();
                var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);bitmap.Render(window);
                int stride=bitmap.PixelWidth*4;byte[] pixels=new byte[stride*bitmap.PixelHeight];bitmap.CopyPixels(pixels,stride,0);
                for(int i=0;i<pixels.Length;i+=4)if(pixels[i]!=0 || pixels[i+1]!=0 || pixels[i+2]!=0)throw new Exception("Non-black pixels present");
                overlay.Update(ProtectionState.Normal,540,new(),new(),1);Assert(overlay.ActiveWindow==null,"Overlay remained visible");
            });
            Check("Countdown and moving telemetry overlay render",()=>
            {
                using var overlay=new OverlayManager();overlay.Update(ProtectionState.Countdown,10,new(){Aircraft="TEST"},new(),0);Assert(overlay.ActiveWindow?.IsVisible==true,"Countdown missing");
                overlay.Update(ProtectionState.Black,60,new(){Aircraft="TEST"},new(){MiniTelemetry=true},25,"50 NM MOCK",true);Assert(overlay.ActiveWindow?.IsVisible==true,"Telemetry missing");
            });
            app.Shutdown();
        }
        Console.WriteLine($"{passed} passed, {failed} failed");return failed==0?0:1;
    }
}

internal sealed class StubHttpHandler(Func<System.Net.Http.HttpRequestMessage,System.Net.Http.HttpResponseMessage> respond) : System.Net.Http.HttpMessageHandler
{
    public int Requests { get; private set; }
    protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken cancellationToken)
    {Requests++;cancellationToken.ThrowIfCancellationRequested();return Task.FromResult(respond(request));}
}
