using System.Net.Http.Json;
using System.Text.Json;
using PlayerOne.Windows.Models;

namespace PlayerOne.Windows.Services;
public sealed class PlayerOneApi {
  public const string DefaultApi = "https://playeronetv.site";
  readonly HttpClient http = new(){Timeout=TimeSpan.FromSeconds(25)};
  static readonly JsonSerializerOptions json = new(){PropertyNameCaseInsensitive=true};

  public async Task<DeviceAuth> BootstrapAsync() {
    var response=await http.PostAsJsonAsync($"{DefaultApi}/api/device/bootstrap",new { hardwareId=DeviceIdentity.HardwareId(), label=DeviceIdentity.Label() });
    response.EnsureSuccessStatusCode();
    using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var r=doc.RootElement;
    string? S(string n)=>r.TryGetProperty(n,out var v)&&v.ValueKind!=JsonValueKind.Null?v.ToString():null;
    return new(DefaultApi,S("deviceId")??throw new InvalidDataException("Missing Device ID"),S("deviceKey")??throw new InvalidDataException("Missing Device Key"),S("status"),S("trialExpiresAt"),S("expiresAt"));
  }
  public async Task<JsonDocument> GetPlaylistsAsync(DeviceAuth a) =>
    await GetAsync($"{a.Api}/api/device/playlists?deviceId={Uri.EscapeDataString(a.DeviceId)}&deviceKey={Uri.EscapeDataString(a.DeviceKey)}");
  public async Task<JsonDocument> GetStatusAsync(DeviceAuth a) =>
    await GetAsync($"{a.Api}/api/devices/status?deviceId={Uri.EscapeDataString(a.DeviceId)}&deviceKey={Uri.EscapeDataString(a.DeviceKey)}");
  public async Task<JsonDocument> GetPlaylistConfigAsync(DeviceAuth a,long id) =>
    await GetAsync($"{a.Api}/api/device/playlist-config?deviceId={Uri.EscapeDataString(a.DeviceId)}&deviceKey={Uri.EscapeDataString(a.DeviceKey)}&playlistId={id}");
  async Task<JsonDocument> GetAsync(string url) {
    using var res=await http.GetAsync(url); res.EnsureSuccessStatusCode();
    return JsonDocument.Parse(await res.Content.ReadAsStringAsync());
  }
}
