using System.Text.Json;
using PlayerOne.Windows.Models;
namespace PlayerOne.Windows.Services;
public static class PlayerStateStore {
 static string Root=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Player One","state");
 static string Scope(DeviceAuth a,long? playlist)=>Sanitize(a.DeviceId)+"_p"+(playlist??-1);
 static string PathFor(DeviceAuth a,long? p,string kind)=>Path.Combine(Root,$"{Scope(a,p)}_{kind}.json");
 public static List<RecentEntry> Recents(DeviceAuth a,long? p)=>Read<RecentEntry>(PathFor(a,p,"recents")).Take(120).ToList();
 public static List<FavoriteEntry> Favorites(DeviceAuth a,long? p)=>Read<FavoriteEntry>(PathFor(a,p,"favorites")).Take(240).ToList();
 public static void AddRecent(DeviceAuth a,long? p,RecentEntry e){var x=Recents(a,p);x.RemoveAll(i=>!string.IsNullOrWhiteSpace(e.StreamUrl)&&i.StreamUrl==e.StreamUrl);x.Insert(0,e with{UpdatedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()});Write(PathFor(a,p,"recents"),x.Take(120));}
 public static bool Meaningful(RecentEntry e)=>e.PositionMs>=10000&&(e.DurationMs<=0||(e.DurationMs-e.PositionMs>60000&&(double)e.PositionMs/Math.Max(1,e.DurationMs)<.95));
 public static bool ToggleFavorite(DeviceAuth a,long? p,FavoriteEntry e){var x=Favorites(a,p);var i=x.FindIndex(v=>v.Type==e.Type&&v.Id==e.Id);var add=i<0;if(add)x.Insert(0,e with{UpdatedAt=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()});else x.RemoveAt(i);Write(PathFor(a,p,"favorites"),x.Take(240));return add;}
 static List<T> Read<T>(string p){try{return File.Exists(p)?JsonSerializer.Deserialize<List<T>>(File.ReadAllText(p))??[]:[];}catch{return [];}}
 static void Write<T>(string p,IEnumerable<T> x){Directory.CreateDirectory(Root);File.WriteAllText(p,JsonSerializer.Serialize(x));}
 static string Sanitize(string s)=>string.Concat(s.Take(80).Select(ch=>char.IsLetterOrDigit(ch)||ch is '_' or '-'?ch:'_'));
}
