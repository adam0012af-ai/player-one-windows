using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using PlayerOne.Windows.Models;

namespace PlayerOne.Windows.Services;
public sealed class PlayerOneApi {
  public const string DefaultApi = "https://playeronetv.site";
  readonly HttpClient http = new(){Timeout=TimeSpan.FromSeconds(25)};

  public async Task<DeviceAuth> BootstrapAsync() {
    using var req=JsonRequest(HttpMethod.Post,$"{DefaultApi}/api/device/bootstrap",new {
      appCode="player-one",
      hardwareId=DeviceIdentity.HardwareId(),
      label=DeviceIdentity.Label()
    });
    using var response=await http.SendAsync(req);
    var raw=await response.Content.ReadAsStringAsync();
    if(!response.IsSuccessStatusCode)throw ApiError(raw,(int)response.StatusCode);
    using var doc=JsonDocument.Parse(raw);
    var root=doc.RootElement;
    if(root.TryGetProperty("ok",out var ok)&&ok.ValueKind==JsonValueKind.False)throw ApiError(raw,(int)response.StatusCode);
    if(!root.TryGetProperty("device",out var r)||r.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Device response missing");
    string? S(string n)=>r.TryGetProperty(n,out var v)&&v.ValueKind!=JsonValueKind.Null?v.ToString().Trim():null;
    var id=S("deviceId"); var key=S("deviceKey");
    if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(key))throw new InvalidDataException("Invalid device response");
    return new(DefaultApi,id,key,S("status"),S("trialExpiresAt"),S("expiresAt"));
  }

  string AuthQuery(DeviceAuth a)=>$"deviceId={Uri.EscapeDataString(a.DeviceId)}&deviceKey={Uri.EscapeDataString(a.DeviceKey)}";
  public Task<JsonDocument> GetPlaylistsAsync(DeviceAuth a)=>GetAsync($"{a.Api}/api/device/playlists?{AuthQuery(a)}");
  public Task<JsonDocument> GetStatusAsync(DeviceAuth a)=>GetAsync($"{a.Api}/api/devices/status?{AuthQuery(a)}");
  public Task<JsonDocument> GetPlaylistConfigAsync(DeviceAuth a,long id)=>GetAsync($"{a.Api}/api/device/playlist-config?{AuthQuery(a)}&playlistId={id}");

  public async Task<long> AddM3uAsync(DeviceAuth a,string name,string url) {
    RequireHttpUrl(url,"M3U URL");
    using var d=await PostAsync($"{a.Api}/api/device/playlists",new {deviceId=a.DeviceId,deviceKey=a.DeviceKey,type="m3u",name=string.IsNullOrWhiteSpace(name)?"My Playlist":name.Trim(),url=url.Trim()});
    return PlaylistId(d);
  }
  public async Task<long> AddXtreamAsync(DeviceAuth a,string name,string host,string username,string password) {
    RequireHttpUrl(host,"Server URL");
    using var d=await PostAsync($"{a.Api}/api/device/playlists",new {deviceId=a.DeviceId,deviceKey=a.DeviceKey,type="xtream",name=string.IsNullOrWhiteSpace(name)?"Xtream":name.Trim(),host=host.Trim().TrimEnd('/'),username=username.Trim(),password});
    return PlaylistId(d);
  }
  public async Task<bool> UpdateM3uAsync(DeviceAuth a,long id,string name,string url) {
    RequireHttpUrl(url,"M3U URL");
    return await UpdateAsync(a,new {deviceId=a.DeviceId,deviceKey=a.DeviceKey,playlistId=id,type="m3u",name=string.IsNullOrWhiteSpace(name)?"My Playlist":name.Trim(),url=url.Trim()});
  }
  public async Task<bool> UpdateXtreamAsync(DeviceAuth a,long id,string name,string host,string username,string password) {
    RequireHttpUrl(host,"Server URL");
    return await UpdateAsync(a,new {deviceId=a.DeviceId,deviceKey=a.DeviceKey,playlistId=id,type="xtream",name=string.IsNullOrWhiteSpace(name)?"Xtream":name.Trim(),host=host.Trim().TrimEnd('/'),username=username.Trim(),password});
  }
  async Task<bool> UpdateAsync(DeviceAuth a,object payload) {
    using var req=JsonRequest(HttpMethod.Post,$"{a.Api}/api/device/playlists/update",payload);
    using var res=await http.SendAsync(req);
    if((int)res.StatusCode is 404 or 405)return false;
    var raw=await res.Content.ReadAsStringAsync();
    if(!res.IsSuccessStatusCode)throw ApiError(raw,(int)res.StatusCode);
    using var d=JsonDocument.Parse(raw);
    return d.RootElement.TryGetProperty("ok",out var ok)&&ok.GetBoolean();
  }
  public async Task<JsonDocument> ProviderLoginAsync(DeviceAuth a,string code,string username,string password) =>
    await PostAsync($"{a.Api}/api/provider/login",new {deviceId=a.DeviceId,deviceKey=a.DeviceKey,code=code.Trim(),username=username.Trim(),password});
  public Task<JsonDocument> GetProviderAccessAsync(DeviceAuth a)=>GetAsync($"{a.Api}/api/provider/access?{AuthQuery(a)}");
  public Task<JsonDocument> GetContentConfigAsync(DeviceAuth a,long id)=>GetAsync($"{a.Api}/api/device/content-config?{AuthQuery(a)}&playlistId={id}");

  public async Task DeletePlaylistAsync(DeviceAuth a,long id) {
    using var d=await PostAsync($"{a.Api}/api/device/playlists/delete",new {deviceId=a.DeviceId,deviceKey=a.DeviceKey,playlistId=id});
    if(!d.RootElement.TryGetProperty("ok",out var ok)||!ok.GetBoolean())throw new InvalidDataException("Playlist was not deleted");
  }

  static long PlaylistId(JsonDocument d) {
    if(!d.RootElement.TryGetProperty("playlist",out var p)||!p.TryGetProperty("id",out var id)||!id.TryGetInt64(out var value))throw new InvalidDataException("Missing playlist id");
    return value;
  }
  static void RequireHttpUrl(string value,string label) {
    if(!Uri.TryCreate(value.Trim(),UriKind.Absolute,out var u)||(u.Scheme!="http"&&u.Scheme!="https"))throw new ArgumentException($"{label} must be a valid HTTP/HTTPS URL");
  }
  HttpRequestMessage JsonRequest(HttpMethod method,string url,object payload) {
    var req=new HttpRequestMessage(method,url);
    req.Headers.UserAgent.ParseAdd("PlayerOne/Windows");
    req.Content=new StringContent(JsonSerializer.Serialize(payload),Encoding.UTF8,"application/json");
    return req;
  }
  async Task<JsonDocument> PostAsync(string url,object payload) {
    using var req=JsonRequest(HttpMethod.Post,url,payload);
    using var res=await http.SendAsync(req);
    var raw=await res.Content.ReadAsStringAsync();
    if(!res.IsSuccessStatusCode)throw ApiError(raw,(int)res.StatusCode);
    return JsonDocument.Parse(raw);
  }
  async Task<JsonDocument> GetAsync(string url) {
    using var req=new HttpRequestMessage(HttpMethod.Get,url);req.Headers.UserAgent.ParseAdd("PlayerOne/Windows");
    using var res=await http.SendAsync(req);var raw=await res.Content.ReadAsStringAsync();
    if(!res.IsSuccessStatusCode)throw ApiError(raw,(int)res.StatusCode);
    return JsonDocument.Parse(raw);
  }
  static Exception ApiError(string raw,int code) {
    try { using var d=JsonDocument.Parse(raw);if(d.RootElement.TryGetProperty("error",out var e)&&!string.IsNullOrWhiteSpace(e.ToString()))return new InvalidOperationException(e.ToString()); } catch {}
    return new HttpRequestException($"Player One API HTTP {code}");
  }
}
