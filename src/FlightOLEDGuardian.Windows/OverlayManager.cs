using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using FlightOLEDGuardian.Core;
using Forms=System.Windows.Forms;

namespace FlightOLEDGuardian.Windows;

public sealed class OverlayManager : IDisposable
{
    internal Window? ActiveWindow => window;
    private Window? window;
    private TextBlock? label;
    private ProtectionState shown;
    private bool telemetry;
    private Window? notification;
    private System.Windows.Threading.DispatcherTimer? notificationTimer;
    public string CaptureStatus { get; private set; }="Capture exclusion not tested";
    public bool CaptureExclusionAccepted { get; private set; }
    public event Action? Skip;
    public event Action? Snooze;
    public event Action? Wake;
    public void Update(ProtectionState state,double seconds,FlightTelemetry flight,GuardianSettings settings,double now,string? todText=null,bool todClose=false)
    {
        if(state is not (ProtectionState.Black or ProtectionState.Countdown)) {Hide();return;}
        if(window==null || state!=shown || telemetry!=settings.MiniTelemetry)
        {
            Hide();shown=state;telemetry=settings.MiniTelemetry;
            window=new Window{Title="FlightOLED Guardian Protection",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,ShowInTaskbar=false,ShowActivated=false,Topmost=true,Background=Brushes.Black};
            var panel=new StackPanel{Margin=new Thickness(28),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};
            label=new TextBlock{Foreground=new SolidColorBrush(Color.FromRgb(55,103,108)),FontSize=22,TextWrapping=TextWrapping.Wrap};panel.Children.Add(label);
            if(state==ProtectionState.Countdown)
            {
                panel.Children.Add(Button(UiText.Get("SKIP ONCE",settings.Language),()=>Skip?.Invoke()));panel.Children.Add(Button(UiText.Get("SNOOZE 10 MIN",settings.Language),()=>Snooze?.Invoke()));
                window.Width=360;window.Height=220;
            }
            else {window.PreviewKeyDown+=(_,_)=>Wake?.Invoke();window.PreviewMouseDown+=(_,_)=>Wake?.Invoke();}
            window.Content=panel;
            var screen=Forms.Screen.AllScreens[Math.Clamp(settings.MonitorIndex,0,Forms.Screen.AllScreens.Length-1)].Bounds;
            window.SourceInitialized+=(_,_)=>
            {
                var h=new WindowInteropHelper(window).Handle;
                var source=HwndSource.FromHwnd(h);source?.AddHook(Hook);
                // WS_EX_NOACTIVATE: showing protection must never steal simulator focus.
                SetWindowLongPtr(h,-20,GetWindowLongPtr(h,-20)|0x08000000);
                if(state==ProtectionState.Black)SetWindowPos(h,new nint(-1),screen.X,screen.Y,screen.Width,screen.Height,0x0010);
                else SetWindowPos(h,new nint(-1),screen.Right-400,screen.Top+40,360,220,0x0010);
                bool excluded=SetWindowDisplayAffinity(h,settings.ExcludeFromCapture?0x11u:0u);
                CaptureExclusionAccepted=excluded && settings.ExcludeFromCapture;
                CaptureStatus=excluded?"Windows accepted display affinity; OBS/capture hardware still requires a recording test":"Windows refused display affinity; capture exclusion unavailable";
            };
            window.Show();
        }
        if(label!=null)
        {
            label.Visibility=state==ProtectionState.Black && !settings.MiniTelemetry ? Visibility.Collapsed : Visibility.Visible;
            label.Text=state==ProtectionState.Countdown?$"{UiText.Get("OLED PROTECTION",settings.Language)}\n{(settings.Language=="tr"?$"{Math.Ceiling(seconds):0} saniye sonra siyah ekran":$"Black screen in {Math.Ceiling(seconds):0} seconds")}":$"{flight.Aircraft}\n{flight.AltitudeMSL:0} FT · {flight.GroundSpeed:0} KT\nAP {(flight.Autopilot?"ON":"OFF")} · {flight.VerticalSpeed:0} FPM\n{todText??UiText.Get("TOD unavailable",settings.Language)}";
            label.Foreground=new SolidColorBrush(todClose?Color.FromRgb(139,93,34):Color.FromRgb(55,103,108));
            if(state==ProtectionState.Black && settings.MiniTelemetry)
            {label.RenderTransform=new TranslateTransform(Math.Sin(Math.Floor(now/20)*2.1)*120,Math.Cos(Math.Floor(now/20)*1.3)*120);}
        }
    }
    public void ShowTod(string message,GuardianSettings settings)
    {
        notificationTimer?.Stop();notification?.Close();
        var screen=Forms.Screen.AllScreens[Math.Clamp(settings.MonitorIndex,0,Forms.Screen.AllScreens.Length-1)].WorkingArea;
        notification=new Window{Title="FlightOLED Guardian TOD",Width=480,Height=170,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,Topmost=true,ShowActivated=false,ShowInTaskbar=false,Background=new SolidColorBrush(Color.FromRgb(18,23,31)),Content=new TextBlock{Text=UiText.Get("TOP OF DESCENT APPROACHING",settings.Language)+"\n\n"+message,Foreground=new SolidColorBrush(Color.FromRgb(218,157,66)),FontSize=21,Margin=new Thickness(24),TextWrapping=TextWrapping.Wrap}};
        notification.SourceInitialized+=(_,_)=>{var handle=new WindowInteropHelper(notification).Handle;SetWindowLongPtr(handle,-20,GetWindowLongPtr(handle,-20)|0x08000000);SetWindowPos(handle,new nint(-1),screen.Right-510,screen.Top+40,480,170,0x0010);SetWindowDisplayAffinity(handle,settings.ExcludeFromCapture?0x11u:0u);};
        notification.Show();notificationTimer=new(){Interval=TimeSpan.FromSeconds(12)};
        notificationTimer.Tick+=(_,_)=>CloseNotification();notificationTimer.Start();
    }
    public void CloseNotification(){notificationTimer?.Stop();notificationTimer=null;notification?.Close();notification=null;}
    private nint Hook(nint h,int msg,nint w,nint l,ref bool handled)
    {if(msg==0x0021){handled=true;return new nint(3);}return 0;} // MA_NOACTIVATE
    private static Button Button(string text,Action action) {var b=new Button{Content=text,Margin=new Thickness(0,12,0,0),Padding=new Thickness(10)};b.Click+=(_,_)=>action();return b;}
    public void Hide() {var old=window;window=null;label=null;old?.Close();}
    public void Dispose(){Hide();CloseNotification();}
    [DllImport("user32.dll",SetLastError=true)] private static extern bool SetWindowDisplayAffinity(nint h,uint affinity);
    [DllImport("user32.dll")] private static extern nint GetWindowLongPtr(nint h,int index);
    [DllImport("user32.dll")] private static extern nint SetWindowLongPtr(nint h,int index,nint value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint h,nint after,int x,int y,int width,int height,uint flags);
}
