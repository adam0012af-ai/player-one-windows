using System.Net;
namespace PlayerOne.Windows.Services;
public sealed class M3uClient {
 readonly HttpClient http=new(){Timeout=TimeSpan.FromSeconds(90)};
 public async Task<List<XtreamItem>> LoadAsync(string url,string kind){
  if(string.IsNullOrWhiteSpace(url))throw new InvalidDataException("Playlist URL is missing");
  using var req=new HttpRequestMessage(HttpMethod.Get,url);
  req.Headers.UserAgent.ParseAdd("PlayerOne/Windows");
  req.Headers.Accept.ParseAdd("application/vnd.apple.mpegurl");
  req.Headers.Accept.ParseAdd("application/x-mpegURL");
  req.Headers.Accept.ParseAdd("text/plain");
  using var response=await http.SendAsync(req,HttpCompletionOption.ResponseHeadersRead);
  response.EnsureSuccessStatusCode();
  await using var stream=await response.Content.ReadAsStreamAsync();
  using var reader=new StreamReader(stream);
  var output=new List<XtreamItem>();string? title=null,logo=null;var group="Other";var index=0;
  while(await reader.ReadLineAsync() is string raw){
   var line=raw.Trim().TrimStart('\uFEFF');if(line.Length==0)continue;
   if(line.StartsWith("#EXTINF",StringComparison.OrdinalIgnoreCase)){title=AfterComma(line);group=Attr(line,"group-title")??group;logo=Attr(line,"tvg-logo");continue;}
   if(line.StartsWith("#EXTGRP:",StringComparison.OrdinalIgnoreCase)){group=line[(line.IndexOf(':')+1)..].Trim();continue;}
   if(line.StartsWith("#"))continue;
   var resolved=Resolve(url,line);if(resolved is null)continue;index++;
   var name=string.IsNullOrWhiteSpace(title)?$"Channel {index}":title!;
   var detected=DetectKind(name,group,resolved);
   if(detected==kind)output.Add(new($"m3u-{kind}-{index}-{resolved.GetHashCode()}",name,group,logo,resolved,kind,0));
   title=null;logo=null;group="Other";
  }
  return output.GroupBy(x=>x.StreamUrl,StringComparer.OrdinalIgnoreCase).Select(x=>x.First()).ToList();
 }
 static string? Resolve(string playlist,string raw){if(Uri.TryCreate(raw,UriKind.Absolute,out var a)&&(a.Scheme=="http"||a.Scheme=="https"))return a.ToString();if(Uri.TryCreate(new Uri(playlist),raw,out var r))return r.ToString();return null;}
 static string AfterComma(string s){var i=s.IndexOf(',');return i>=0?s[(i+1)..].Trim():"Channel";}
 static string? Attr(string s,string key){var token=key+"=";var i=s.IndexOf(token,StringComparison.OrdinalIgnoreCase);if(i<0)return null;i+=token.Length;if(i>=s.Length)return null;if(s[i]=='"'){var e=s.IndexOf('"',i+1);return e>i?s[(i+1)..e].Trim():null;}var end=s.IndexOfAny(new[]{' ',','},i);if(end<0)end=s.Length;return s[i..end].Trim('"','\'');}
 static string DetectKind(string name,string group,string url){var path=url.ToLowerInvariant().Split('?')[0];var text=(name+" "+group+" "+path).ToLowerInvariant();if(path.Contains("/series/")||System.Text.RegularExpressions.Regex.IsMatch(name,@"(?i)\bS\d{1,2}\s*E\d{1,3}\b"))return "series";if(path.Contains("/movie/")||path.Contains("/vod/")||new[]{".mp4",".mkv",".avi",".mov",".m4v",".webm"}.Any(path.EndsWith))return "movie";var vod=new[]{"movie","movies","vod","cinema","film","افلام","أفلام","فيلم","series","مسلسل","مسلسلات"};if(path.Contains("/live/")||path.EndsWith(".m3u8")||path.EndsWith(".ts")||!vod.Any(text.Contains))return "live";return "movie";}
}