using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]
[assembly: AssemblyProduct("FlightOLED Guardian")]
[assembly: AssemblyTitle("FlightOLED Guardian Setup")]

// .NET Framework bootstrapper: runs before the application's .NET 8 prerequisite is installed.
internal static class GuardianSetup
{
    const string Key = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FlightOLEDGuardian";
    const string Exe = "FlightOLEDGuardian.App.exe";
    static bool turkish;
    static string T(string en,string tr){return turkish?tr:en;}
    static string Self {get{return Assembly.GetExecutingAssembly().Location;}}
    static string DefaultFolder {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","FlightOLEDGuardian");}}
    static string MenuLink {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),"Programs","FlightOLED Guardian.lnk");}}
    static string DesktopLink {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"FlightOLED Guardian.lnk");}}
    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            bool selftest=args.Length==2 && args[0]=="--self-test";
            if(selftest)
            {
                string folder=Path.GetFullPath(args[1]);Directory.CreateDirectory(folder);Extract(folder);
                if(!File.Exists(Path.Combine(folder,Exe)) || !File.Exists(Path.Combine(folder,"FlightOLEDGuardian.App.runtimeconfig.json")))throw new IOException("Missing app payload or runtime configuration");
                try{SafeTarget(folder,"../escape.txt");throw new IOException("Path traversal accepted");}catch(ArgumentException){}
                File.WriteAllText(Path.Combine(folder,"setup-test.txt"),"PASS embedded payload extraction\nPASS archive path traversal rejected\nPASS Windows executable and runtime configuration present\n");
            }
            if(args.Length==2 && args[0]=="--remove"){Remove(args[1]);return 0;}
            bool uninstall=args.Contains("--uninstall");
            var app=new Application();var window=new Window{Title=uninstall?"FlightOLED Guardian · Uninstall":"FlightOLED Guardian · Setup",Width=600,Height=470,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterScreen,Background=new SolidColorBrush(Color.FromRgb(12,18,27)),Foreground=Brushes.White};
            using(var icon=Assembly.GetExecutingAssembly().GetManifestResourceStream("Guardian.ico"))window.Icon=System.Windows.Media.Imaging.BitmapFrame.Create(icon);
            var panel=new StackPanel{Margin=new Thickness(32)};window.Content=panel;
            panel.Children.Add(new TextBlock{Text="FLIGHTOLED GUARDIAN",FontSize=27,FontWeight=FontWeights.Bold,Foreground=Brushes.Turquoise});
            var lang=new ComboBox{Margin=new Thickness(0,18,0,12),ItemsSource=new[]{"English","Türkçe"},SelectedIndex=0};panel.Children.Add(lang);
            var description=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};panel.Children.Add(description);
            var path=new TextBlock{Text=DefaultFolder,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue};panel.Children.Add(path);
            var desktop=new CheckBox{Margin=new Thickness(0,15,0,10),Foreground=Brushes.White,IsChecked=false};panel.Children.Add(desktop);
            var button=new Button{Margin=new Thickness(0,12,0,12),Padding=new Thickness(18,10,18,10),HorizontalAlignment=HorizontalAlignment.Left};panel.Children.Add(button);
            var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue};panel.Children.Add(status);
            Action labels=()=>{description.Text=uninstall?T("Remove the application. Your settings and flight reports are kept. Disable 'Start with MSFS' inside Guardian before uninstalling.","Uygulama kaldırılır; ayarlar ve uçuş raporları korunur. Kaldırmadan önce Guardian ayarlarından MSFS ile başlatmayı kapatın."):T("Install v0.3.0 for your Windows account. Includes Start menu shortcut and Windows uninstall entry. Requires .NET 8 Desktop Runtime (x64). Close Guardian before installing or updating.","v0.3.0 bu Windows hesabına kurulur. Başlat menüsü kısayolu ve Windows kaldırma kaydı eklenir. .NET 8 Desktop Runtime (x64) gerekir. Kurulum veya güncellemeden önce Guardian'ı kapatın.");desktop.Content=T("Create a desktop shortcut","Masaüstü kısayolu oluştur");desktop.Visibility=uninstall?Visibility.Collapsed:Visibility.Visible;button.Content=uninstall?T("Uninstall","Kaldır"):T("Install","Kur");};
            lang.SelectionChanged+=delegate{turkish=lang.SelectedIndex==1;labels();};labels();
            button.Click+=delegate
            {
                button.IsEnabled=false;
                try
                {
                    if(uninstall)
                    {
                        string installed=Path.GetDirectoryName(Self);
                        if(!Registered(installed))throw new IOException(T("Run the installed uninstaller from Windows Settings.","Windows Ayarları içindeki kurulu kaldırıcıyı kullanın."));
                        string temp=Path.Combine(Path.GetTempPath(),"Guardian-Uninstall-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(Self,temp);
                        Process.Start(new ProcessStartInfo(temp,"--remove \""+installed+"\""){UseShellExecute=true});app.Shutdown();return;
                    }
                    if(!HasRuntime())
                    {
                        status.Text=T("Install .NET 8 Desktop Runtime x64, then run Setup again. The official download page is opening.",".NET 8 Desktop Runtime x64 kurup Setup'ı tekrar çalıştırın. Resmî indirme sayfası açılıyor.");
                        Process.Start(new ProcessStartInfo("https://dotnet.microsoft.com/en-us/download/dotnet/8.0"){UseShellExecute=true});button.IsEnabled=true;return;
                    }
                    bool exists;using(var mutex=new System.Threading.Mutex(false,@"Local\FlightOLEDGuardian",out exists)){if(!exists)throw new IOException(T("Close Guardian using Exit in its tray menu first.","Önce tepsi menüsünden Çıkış ile Guardian'ı kapatın."));}
                    string dest=DefaultFolder;Directory.CreateDirectory(dest);
                    Extract(dest);File.Copy(Self,Path.Combine(dest,"GuardianSetup.exe"),true);
                    Shortcut(MenuLink,Path.Combine(dest,Exe));if(desktop.IsChecked==true)Shortcut(DesktopLink,Path.Combine(dest,Exe));
                    using(var key=Registry.CurrentUser.CreateSubKey(Key))
                    {key.SetValue("DisplayName","FlightOLED Guardian");key.SetValue("DisplayVersion","0.3.0");key.SetValue("Publisher","FlightOLED Guardian");key.SetValue("InstallLocation",dest);key.SetValue("DisplayIcon",Path.Combine(dest,Exe));key.SetValue("UninstallString","\""+Path.Combine(dest,"GuardianSetup.exe")+"\" --uninstall");key.SetValue("NoModify",1);key.SetValue("NoRepair",1);}
                    status.Text=T("Installed. Open FlightOLED Guardian from the Start menu. Settings and reports are preserved on updates.","Kuruldu. Başlat menüsünden FlightOLED Guardian'ı açabilirsiniz. Güncellemelerde ayarlar ve raporlar korunur.");button.Content=T("Done","Tamam");
                }
                catch(Exception error){status.Text=error.Message;button.IsEnabled=true;}
            };
            if(selftest)window.Loaded+=delegate
            {
                window.UpdateLayout();var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);image.Render(window);
                var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));using(var file=File.Create(Path.Combine(args[1],"setup-preview.png")))encoder.Save(file);
                File.AppendAllText(Path.Combine(args[1],"setup-test.txt"),"PASS setup window create/render/close\n");window.Close();
            };
            app.Run(window);return 0;
        }
        catch(Exception error){MessageBox.Show(error.Message,"FlightOLED Guardian Setup");return 1;}
    }
    static bool HasRuntime()
    {
        string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"dotnet","shared","Microsoft.WindowsDesktop.App");
        return Directory.Exists(root)&&Directory.GetDirectories(root,"8.*").Any();
    }
    static string SafeTarget(string folder,string entry)
    {
        if(string.IsNullOrWhiteSpace(entry)||entry!=Path.GetFileName(entry)||entry.Contains(":"))throw new ArgumentException("Invalid archive path");
        string target=Path.GetFullPath(Path.Combine(folder,entry));
        if(!target.StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Archive escaped installation folder");return target;
    }
    static void Extract(string folder)
    {
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip"))using(var archive=new ZipArchive(stream,ZipArchiveMode.Read))
        {
            foreach(var entry in archive.Entries)SafeTarget(folder,entry.FullName);
            string stage=Path.Combine(folder,".guardian-stage-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
            var changed=new System.Collections.Generic.List<string>();
            try
            {
                foreach(var entry in archive.Entries)using(var input=entry.Open())using(var output=File.Create(SafeTarget(stage,entry.FullName)))input.CopyTo(output);
                foreach(var entry in archive.Entries)
                {
                    string target=SafeTarget(folder,entry.FullName);string backup=SafeTarget(stage,entry.FullName+".previous");
                    if(File.Exists(target))File.Copy(target,backup);
                    changed.Add(entry.FullName);File.Copy(SafeTarget(stage,entry.FullName),target,true);
                }
            }
            catch
            {
                foreach(string name in changed.AsEnumerable().Reverse())
                {
                    string backup=SafeTarget(stage,name+".previous");string target=SafeTarget(folder,name);
                    if(File.Exists(backup))File.Copy(backup,target,true);else File.Delete(target);
                }
                throw;
            }
            finally{foreach(string file in Directory.GetFiles(stage))File.Delete(file);Directory.Delete(stage);}
        }
    }
    static void Shortcut(string path,string target)
    {
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        try{dynamic link=shell.CreateShortcut(path);try{link.TargetPath=target;link.WorkingDirectory=Path.GetDirectoryName(target);link.IconLocation=target+",0";link.Save();}finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);}}finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
    }
    static bool Registered(string folder)
    {using(var key=Registry.CurrentUser.OpenSubKey(Key))return key!=null&&string.Equals(key.GetValue("InstallLocation") as string,Path.GetFullPath(folder),StringComparison.OrdinalIgnoreCase);}
    static void Remove(string folder)
    {
        folder=Path.GetFullPath(folder);if(!Registered(folder))throw new IOException("Installation record does not match");
        bool created;using(var mutex=new System.Threading.Mutex(false,@"Local\FlightOLEDGuardian",out created)){if(!created)throw new IOException("Close Guardian from its tray menu before uninstalling.");}
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.zip"))using(var zip=new ZipArchive(stream,ZipArchiveMode.Read))foreach(var entry in zip.Entries)File.Delete(SafeTarget(folder,entry.FullName));
        File.Delete(Path.Combine(folder,"GuardianSetup.exe"));
        foreach(string shortcut in new[]{MenuLink,DesktopLink})
        {
            if(!File.Exists(shortcut))continue;dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            try{dynamic link=shell.CreateShortcut(shortcut);try{if(string.Equals((string)link.TargetPath,Path.Combine(folder,Exe),StringComparison.OrdinalIgnoreCase))File.Delete(shortcut);}finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);}}finally{System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);}
        }
        Registry.CurrentUser.DeleteSubKey(Key,false);if(!Directory.EnumerateFileSystemEntries(folder).Any())Directory.Delete(folder);
        MessageBox.Show("FlightOLED Guardian removed. Settings and flight reports have been kept.\nFlightOLED Guardian kaldırıldı. Ayarlar ve raporlar korundu.","FlightOLED Guardian");
    }
}
