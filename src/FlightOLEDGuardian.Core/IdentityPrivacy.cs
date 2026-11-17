namespace FlightOLEDGuardian.Core;

public static class IdentityPrivacy
{
    public static bool ShouldMask(bool streaming,bool hideIdentity)=>streaming || hideIdentity;
    public static string Redact(string text,string identity)
    {
        if(string.IsNullOrEmpty(identity))return text;
        return text.Replace(Uri.EscapeDataString(identity),"[hidden]",StringComparison.OrdinalIgnoreCase)
            .Replace(identity,"[hidden]",StringComparison.OrdinalIgnoreCase);
    }
}
