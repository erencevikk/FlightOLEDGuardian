using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace FlightOLEDGuardian.Integrations;

public enum UpdateState { Current, Available, NoRelease }
public record UpdateResult(UpdateState State,string Version,Uri? ReleasePage);
public sealed class UpdateClient : IDisposable
{
    public const string CurrentVersion="0.3.0";
    public const string Repository="erencevikk/FlightOLEDGuardian";
    private readonly HttpClient http;
    private readonly bool owns;
    public UpdateClient(HttpClient? client=null){owns=client==null;http=client??new HttpClient{Timeout=TimeSpan.FromSeconds(12)};}
    public async Task<UpdateResult> CheckAsync(CancellationToken token=default)
    {
        using var request=new HttpRequestMessage(HttpMethod.Get,$"https://api.github.com/repos/{Repository}/releases/latest");
        request.Headers.UserAgent.ParseAdd("FlightOLEDGuardian/"+CurrentVersion);
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response=await http.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false);
        if(response.StatusCode==HttpStatusCode.NotFound)return new(UpdateState.NoRelease,"",null);
        response.EnsureSuccessStatusCode();
        using var stream=await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var limited=new MemoryStream();var buffer=new byte[8192];int read;
        while((read=await stream.ReadAsync(buffer,token).ConfigureAwait(false))>0){if(limited.Length+read>262144)throw new InvalidDataException("Release response too large");limited.Write(buffer,0,read);}
        using var json=JsonDocument.Parse(limited.ToArray());var root=json.RootElement;
        if(root.TryGetProperty("draft",out var draft)&&draft.GetBoolean() || root.TryGetProperty("prerelease",out var pre)&&pre.GetBoolean())return new(UpdateState.NoRelease,"",null);
        string tag=root.GetProperty("tag_name").GetString()??"";
        if(!Version.TryParse(tag.TrimStart('v','V'),out var latest))throw new InvalidDataException("Invalid release version");
        var page=new Uri(root.GetProperty("html_url").GetString()??"");
        if(page.Scheme!="https" || page.Host!="github.com" || !page.AbsolutePath.StartsWith("/"+Repository+"/releases/",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid release page");
        return new(latest>Version.Parse(CurrentVersion)?UpdateState.Available:UpdateState.Current,tag,page);
    }
    public void Dispose(){if(owns)http.Dispose();}
}
