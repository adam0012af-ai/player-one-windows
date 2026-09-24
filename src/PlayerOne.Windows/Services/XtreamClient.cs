using System.Text.Json;
namespace PlayerOne.Windows.Services;
public sealed record XtreamItem(string Id,string Name,string Group,string? Image,string? StreamUrl,string Kind,long AddedAt);
public sealed record MovieInfo(string? Plot,string? Genre,string? Director,string? Cast,string? Rating,string? Year,string? Duration,string? Backdrop,string? Trailer);
public sealed class XtreamClient {
 readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(90)};
 static string Enc(string x)=>Uri.EscapeDataString(x);
 string Api(string h,string u,string p)=>$"{h.TrimEnd('/')}/player_api.php?username={Enc(u)}&password={Enc(p)}";
 public async Task<List<XtreamItem>> LoadAsync(string host,string user,string pass,string kind){
  var baseUrl=Api(host,user,pass);var catAction=kind=="live"?"get_live_categories":kind=="movie"?"get_vod_categories":"get_series_categories";var itemAction=kind=="live"?"get_live_streams":kind=="movie"?"get_vod_streams":"get_series";
  using var catsDoc=JsonDocument.Parse(await http.GetStringAsync($"{baseUrl}&action={catAction}"));var cats=catsDoc.RootElement;var map=new Dictionary<string,string>();foreach(var c in cats.EnumerateArray()){var id=Get(c,"category_id");if(id.Length>0)map[id]=Get(c,"category_name");}
  using var itemsDoc=JsonDocument.Parse(await http.GetStringAsync($"{baseUrl}&action={itemAction}"));var items=itemsDoc.RootElement;var result=new List<XtreamItem>();
  foreach(var x in items.EnumerateArray()){var id=Get(x,kind=="series"?"series_id":"stream_id");if(id.Length==0)continue;var group=map.GetValueOrDefault(Get(x,"category_id"),kind);string? stream=null;if(kind!="series"){var ext=kind=="movie"?Get(x,"container_extension","mp4"):"ts";stream=$"{host.TrimEnd('/')}/{(kind=="movie"?"movie":"live")}/{Enc(user)}/{Enc(pass)}/{id}.{ext}";}result.Add(new(id,Get(x,"name",kind),group,Get(x,kind=="series"?"cover":"stream_icon"),stream,kind,Long(x,"added")));}return result;
 }
 public async Task<MovieInfo> MovieInfoAsync(string h,string u,string p,string id){using var d=JsonDocument.Parse(await http.GetStringAsync($"{Api(h,u,p)}&action=get_vod_info&vod_id={Enc(id)}"));var root=d.RootElement;var i=root.TryGetProperty("info",out var z)?z:root;return new(GetN(i,"plot"),GetN(i,"genre"),GetN(i,"director"),GetN(i,"cast"),GetN(i,"rating"),GetN(i,"releasedate")??GetN(i,"year"),GetN(i,"duration"),GetN(i,"movie_image")??GetN(i,"backdrop_path"),GetN(i,"youtube_trailer"));}
 static string Get(JsonElement e,string n,string fallback="")=>e.TryGetProperty(n,out var v)?v.ToString():fallback;static string? GetN(JsonElement e,string n)=>e.TryGetProperty(n,out var v)&&!string.IsNullOrWhiteSpace(v.ToString())?v.ToString():null;static long Long(JsonElement e,string n)=>e.TryGetProperty(n,out var v)&&long.TryParse(v.ToString(),out var x)?x:0;
}