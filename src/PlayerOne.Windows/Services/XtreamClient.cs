using System.Text.Json;
namespace PlayerOne.Windows.Services;
public sealed record XtreamItem(string Id,string Name,string Group,string? Image,string? StreamUrl,string Kind,long AddedAt);
public sealed class XtreamClient {
 readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(30)};
 static string Enc(string x)=>Uri.EscapeDataString(x);
 public async Task<List<XtreamItem>> LoadAsync(string host,string user,string pass,string kind){
  host=host.TrimEnd('/');var baseUrl=$"{host}/player_api.php?username={Enc(user)}&password={Enc(pass)}";
  var catAction=kind=="live"?"get_live_categories":kind=="movie"?"get_vod_categories":"get_series_categories";
  var itemAction=kind=="live"?"get_live_streams":kind=="movie"?"get_vod_streams":"get_series";
  var cats=JsonDocument.Parse(await http.GetStringAsync($"{baseUrl}&action={catAction}")).RootElement;
  var map=new Dictionary<string,string>();foreach(var c in cats.EnumerateArray()){var id=Get(c,"category_id");if(id.Length>0)map[id]=Get(c,"category_name");}
  var items=JsonDocument.Parse(await http.GetStringAsync($"{baseUrl}&action={itemAction}")).RootElement;var result=new List<XtreamItem>();
  foreach(var x in items.EnumerateArray()){
   var id=Get(x,kind=="series"?"series_id":"stream_id");if(id.Length==0)continue;var group=map.GetValueOrDefault(Get(x,"category_id"),kind);
   string? stream=null;if(kind!="series"){var ext=kind=="movie"?Get(x,"container_extension","mp4"):"ts";stream=$"{host}/{(kind=="movie"?"movie":"live")}/{Enc(user)}/{Enc(pass)}/{id}.{ext}";}
   result.Add(new(id,Get(x,"name",kind),group,Get(x,kind=="series"?"cover":"stream_icon"),stream,kind,Long(x,"added")));
  }return result;
 }
 static string Get(JsonElement e,string n,string fallback="")=>e.TryGetProperty(n,out var v)?v.ToString():fallback;
 static long Long(JsonElement e,string n)=>e.TryGetProperty(n,out var v)&&long.TryParse(v.ToString(),out var x)?x:0;
}
