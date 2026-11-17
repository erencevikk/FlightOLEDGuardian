using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace FlightOLEDGuardian.Windows;

public static class MsfsAutoLaunchService
{
    public const string AddonName="FlightOLED Guardian";
    public static IReadOnlyList<string> FindConfigurationFiles()
    {
        var local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string[] paths=[Path.Combine(local,"Packages","Microsoft.Limitless_8wekyb3d8bbwe","LocalCache","exe.xml"),Path.Combine(local,"Packages","Microsoft.FlightSimulator_8wekyb3d8bbwe","LocalCache","exe.xml"),Path.Combine(roaming,"Microsoft Flight Simulator 2024","exe.xml"),Path.Combine(roaming,"Microsoft Flight Simulator","exe.xml")];
        return paths.Where(p=>File.Exists(p) || Directory.Exists(Path.GetDirectoryName(p))).ToArray();
    }
    public static void Configure(string xmlPath,string executable,bool enabled)
    {
        if(!Path.IsPathFullyQualified(xmlPath) || !Path.GetFileName(xmlPath).Equals("exe.xml",StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Select the simulator's EXE.xml file.");
        if(enabled && (!Path.IsPathFullyQualified(executable) || !File.Exists(executable)))throw new ArgumentException("Guardian executable not found.");
        XDocument document;
        bool existed=File.Exists(xmlPath);
        if(existed)
        {
            using var reader=XmlReader.Create(xmlPath,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2_000_000});
            document=XDocument.Load(reader,LoadOptions.PreserveWhitespace);
            if(document.Root?.Name.LocalName!="SimBase.Document")throw new FormatException("Invalid MSFS EXE.xml root. Existing file has not been modified.");
        }
        else
        {
            if(!enabled)return;
            document=new(new XDeclaration("1.0","utf-8",null),new XElement("SimBase.Document",new XAttribute("Type","Launch"),new XAttribute("version","1,0"),new XElement("Descr","Launch"),new XElement("Filename","EXE.xml"),new XElement("Disabled","False"),new XElement("Launch.ManualLoad","False")));
        }
        var root=document.Root!;
        if(enabled && string.Equals(root.Element("Disabled")?.Value,"True",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("MSFS EXE.xml startup is globally disabled. Existing settings have not been modified.");
        var owned=root.Elements("Launch.Addon").Where(e=>string.Equals(e.Element("Name")?.Value,AddonName,StringComparison.OrdinalIgnoreCase)).ToArray();
        if(!enabled && owned.Length==0)return;
        foreach(var addon in owned)addon.Remove();
        if(enabled)root.Add(new XElement("Launch.Addon",new XElement("Name",AddonName),new XElement("Disabled","False"),new XElement("ManualLoad","False"),new XElement("Path",executable),new XElement("CommandLine","--tray"),new XElement("NewConsole","False")));
        Directory.CreateDirectory(Path.GetDirectoryName(xmlPath)!);
        if(existed)File.Copy(xmlPath,xmlPath+".flightoled-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+"-"+Guid.NewGuid().ToString("N")+".bak");
        string temporary=xmlPath+".flightoled.tmp";
        try
        {
            using(var writer=XmlWriter.Create(temporary,new XmlWriterSettings{Encoding=new UTF8Encoding(false),Indent=true}))document.Save(writer);
            File.Move(temporary,xmlPath,true);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
