using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using FlightOLEDGuardian.Core;

namespace FlightOLEDGuardian.Integrations;

public record PlannedRoute(string Id,IReadOnlyList<GeoPoint> Points,int TodIndex,string Origin="",string Destination="",string Aircraft="");
public static class FlightPlanImport
{
    // SimBrief XML downloaded by the user. No account, network or credentials required.
    public static PlannedRoute ReadSimBrief(string path)
    {
        using var reader=XmlReader.Create(path,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2_000_000});
        return Parse(XDocument.Load(reader),true);
    }
    public static PlannedRoute FromXml(string xmlText,bool requireTod=false)
    {
        using var text=new StringReader(xmlText);
        using var reader=XmlReader.Create(text,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2_000_000});
        return Parse(XDocument.Load(reader),requireTod);
    }
    private static PlannedRoute Parse(XDocument xml,bool requireTod)
    {
        var points=new List<GeoPoint>();int tod=-1;
        foreach(var fix in xml.Descendants("navlog").Elements("fix"))
        {
            var lat=Parse(fix.Element("pos_lat")?.Value);var lon=Parse(fix.Element("pos_long")?.Value);
            if(Math.Abs(lat)>90 || Math.Abs(lon)>180)throw new FormatException("Invalid route coordinates");
            points.Add(new(lat,lon));
            if(string.Equals(fix.Element("ident")?.Value,"TOD",StringComparison.OrdinalIgnoreCase))tod=points.Count-1;
        }
        if(points.Count<2)throw new FormatException("Flight plan contains no usable navigation route.");
        if(requireTod && tod<1)throw new FormatException("This XML contains no usable navlog TOD fix. No TOD has been estimated.");
        string origin=xml.Root?.Element("origin")?.Element("icao_code")?.Value??"";
        string destination=xml.Root?.Element("destination")?.Element("icao_code")?.Value??"";
        string aircraft=xml.Root?.Element("aircraft")?.Element("icaocode")?.Value??"";
        string identity=origin+"|"+destination+"|"+xml.Root?.Element("times")?.Element("sched_out")?.Value+"|"+tod+"|"+string.Join(";",points.Select(p=>FormattableString.Invariant($"{p.Latitude:R},{p.Longitude:R}")));
        string id=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)));
        return new(id,points,tod,origin,destination,aircraft);
    }
    private static double Parse(string? value) => double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var result) && double.IsFinite(result)?result:throw new FormatException("Missing or invalid coordinate");
}
