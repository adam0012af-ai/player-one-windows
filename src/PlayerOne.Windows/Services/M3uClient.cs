namespace PlayerOne.Windows.Services;
public sealed class M3uClient {
 readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(40)};
 public async Task<List<XtreamItem>> LoadAsync(string url,string kind){
  using var req=new HttpRequestMessage(HttpMethod.Get,url);req.Headers.UserAgent.ParseAdd("PlayerOne/Windows");
  var text=await (await http.SendAsync(req)).Content.ReadAsStringAsync();
  return PlaylistParser.ParseM3u(text).Where(x=>x.Kind==kind).Select((x,i)=>new XtreamItem($"m3u-{kind}-{i}-{x.Url.GetHashCode()}",x.Name,x.Group,x.Logo,x.Url,kind,0)).ToList();
 }
}
