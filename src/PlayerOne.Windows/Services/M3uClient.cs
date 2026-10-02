using System.Net;
namespace PlayerOne.Windows.Services;

public sealed class M3uClient
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };

    public async Task<List<XtreamItem>> LoadAsync(string url,string kind)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidDataException("Playlist URL is missing");

        using var req = new HttpRequestMessage(HttpMethod.Get,url);
        req.Headers.UserAgent.ParseAdd("PlayerOne/Windows");
        req.Headers.Accept.ParseAdd("application/vnd.apple.mpegurl");
        req.Headers.Accept.ParseAdd("application/x-mpegURL");
        req.Headers.Accept.ParseAdd("text/plain");

        using var response = await Http.SendAsync(req,HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var reader = new StreamReader(stream, bufferSize: 64 * 1024);

        var output = new List<XtreamItem>();
        var groupOrder = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        string? title = null;
        string? logo = null;
        var group = "Other";
        var index = 0;

        while (await reader.ReadLineAsync().ConfigureAwait(false) is string raw)
        {
            var line = raw.Trim().TrimStart('\uFEFF');
            if (line.Length == 0) continue;

            if (line.StartsWith("#EXTINF",StringComparison.OrdinalIgnoreCase))
            {
                title = AfterComma(line);
                group = Attr(line,"group-title") ?? group;
                logo = Attr(line,"tvg-logo");
                continue;
            }

            if (line.StartsWith("#EXTGRP:",StringComparison.OrdinalIgnoreCase))
            {
                group = line[(line.IndexOf(':') + 1)..].Trim();
                continue;
            }

            if (line.StartsWith("#")) continue;

            var resolved = Resolve(url,line);
            if (resolved is null) continue;

            index++;
            var name = string.IsNullOrWhiteSpace(title) ? $"Channel {index}" : title!;
            var cleanGroup = string.IsNullOrWhiteSpace(group) ? "Other" : group.Trim();
            if (!groupOrder.ContainsKey(cleanGroup)) groupOrder[cleanGroup] = groupOrder.Count;
            var detected = DetectKind(name,cleanGroup,resolved);

            if (detected == kind)
            {
                output.Add(new XtreamItem(
                    $"m3u-{kind}-{index}-{resolved.GetHashCode()}",
                    name,
                    cleanGroup,
                    logo,
                    resolved,
                    kind,
                    0,
                    groupOrder[cleanGroup]));
            }

            title = null;
            logo = null;
            group = "Other";
        }

        return output
            .GroupBy(x => x.StreamUrl,StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
    }

    static string? Resolve(string playlist,string raw)
    {
        if (Uri.TryCreate(raw,UriKind.Absolute,out var a) && (a.Scheme == "http" || a.Scheme == "https")) return a.ToString();
        if (Uri.TryCreate(new Uri(playlist),raw,out var r)) return r.ToString();
        return null;
    }

    static string AfterComma(string s)
    {
        var i = s.IndexOf(',');
        return i >= 0 ? s[(i + 1)..].Trim() : "Channel";
    }

    static string? Attr(string s,string key)
    {
        var token = key + "=";
        var i = s.IndexOf(token,StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        i += token.Length;
        if (i >= s.Length) return null;
        if (s[i] == '"')
        {
            var e = s.IndexOf('"',i + 1);
            return e > i ? s[(i + 1)..e].Trim() : null;
        }
        var end = s.IndexOfAny(new[] { ' ', ',' },i);
        if (end < 0) end = s.Length;
        return s[i..end].Trim('"','\'');
    }

    // Keep the same classification rules as the current Android catalog.
    static string DetectKind(string name,string group,string url)
    {
        var path = url.ToLowerInvariant().Split('?')[0];
        var title = name.ToLowerInvariant();
        var groupText = group.ToLowerInvariant();
        var episodeSignal = System.Text.RegularExpressions.Regex.IsMatch(name,@"(?i)(?:\bS\d{1,2}\s*E\d{1,3}\b|season\s*\d+.{0,12}(?:episode|ep)\s*\d+|موسم\s*\d+.{0,12}حلقة\s*\d+)");

        if (path.Contains("/series/") || episodeSignal) return "series";
        if (path.Contains("/movie/") || path.Contains("/vod/")) return "movie";
        if (path.Contains("/live/") || path.EndsWith(".m3u8") || path.EndsWith(".ts")) return "live";

        if (new[] { ".mp4",".mkv",".avi",".mov",".m4v",".webm" }.Any(path.EndsWith)) return episodeSignal ? "series" : "movie";

        var movieWords = new[] { "movie","movies","vod","cinema","film","افلام","أفلام","فيلم" };
        var seriesWords = new[] { "series","serial","show","shows","tv show","مسلسل","مسلسلات","موسم","حلقة" };
        var channelWords = new[] { "bein","channel","live","sports","sport","news","tv","4k","uhd","fhd" };
        var numberedChannel = System.Text.RegularExpressions.Regex.IsMatch(name.Trim(),@"(?i)(?:hd|sd|ch|channel)\s*[-_]?\s*\d{1,2}$");
        var channelLike = numberedChannel || channelWords.Any(title.Contains);

        if (seriesWords.Any(w => groupText.Contains(w) || title.Contains(w))) return channelLike ? "live" : "series";
        if (movieWords.Any(w => groupText.Contains(w) || title.Contains(w))) return channelLike ? "live" : "movie";
        return "live";
    }
}
