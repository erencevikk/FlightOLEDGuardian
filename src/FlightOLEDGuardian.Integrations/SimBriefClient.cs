using System.Net;
using System.Text;

namespace FlightOLEDGuardian.Integrations;

public sealed class SimBriefClient : IDisposable
{
    private readonly HttpClient client;
    private readonly bool ownsClient;
    public SimBriefClient(HttpClient? client=null){ownsClient=client==null;this.client=client??new HttpClient{Timeout=TimeSpan.FromSeconds(20)};}
    public static Uri BuildUri(string identity,bool pilotId)
    {
        identity=identity.Trim();
        if(identity.Length is <1 or >100 || identity.Any(char.IsControl) || pilotId && (identity.Length>7 || identity.Any(c=>c is <'0' or >'9')))
            throw new ArgumentException("Enter a Navigraph Alias or a valid 1–7 digit SimBrief Pilot ID.");
        return new Uri("https://www.simbrief.com/api/xml.fetcher.php?"+(pilotId?"userid=":"username=")+Uri.EscapeDataString(identity)+"&client=FlightOLEDGuardian-0.2");
    }
    // This method is invoked only by the user's import button. Never poll this API.
    public async Task<PlannedRoute> FetchAsync(string identity,bool pilotId,CancellationToken token=default)
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));token=timeout.Token;
        using var request=new HttpRequestMessage(HttpMethod.Get,BuildUri(identity,pilotId));
        using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false);
        if(response.StatusCode==HttpStatusCode.BadRequest)throw new InvalidOperationException("SimBrief user or generated flight plan not found. Check your Alias/Pilot ID and generate an OFP first.");
        response.EnsureSuccessStatusCode();
        if(response.Content.Headers.ContentLength>2_000_000)throw new InvalidDataException("SimBrief response exceeds the size limit.");
        using var stream=await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var buffer=new MemoryStream();byte[] chunk=new byte[8192];int count;
        while((count=await stream.ReadAsync(chunk,token).ConfigureAwait(false))>0)
        {if(buffer.Length+count>2_000_000)throw new InvalidDataException("SimBrief response exceeds the size limit.");buffer.Write(chunk,0,count);}
        return FlightPlanImport.FromXml(Encoding.UTF8.GetString(buffer.ToArray()));
    }
    public void Dispose(){if(ownsClient)client.Dispose();}
}
