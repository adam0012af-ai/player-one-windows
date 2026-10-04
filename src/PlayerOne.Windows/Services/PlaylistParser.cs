using System.Text.RegularExpressions;

namespace PlayerOne.Windows.Services;

public sealed record MediaEntry(string Name, string Group, string? Logo, string Url, string Kind);

public static class PlaylistParser
{
    private static readonly Regex AttrRegexValue =
        new Regex("([\\w-]+)=\"([^\"]*)\"", RegexOptions.Compiled);

    private static readonly Regex EpisodeRegexValue =
        new Regex(
            @"(?:\bS\d{1,2}\s*E\d{1,3}\b|season\s*\d+.{0,12}(?:episode|ep)\s*\d+|موسم\s*\d+.{0,12}حلقة\s*\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

    public static List<MediaEntry> ParseM3u(string text)
    {
        var list = new List<MediaEntry>();
        string? title = null;
        string? logo = null;
        var group = "Other";

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                var attrs = AttrRegexValue
                    .Matches(line)
                    .Cast<Match>()
                    .ToDictionary(
                        x => x.Groups[1].Value,
                        x => x.Groups[2].Value,
                        StringComparer.OrdinalIgnoreCase
                    );

                var comma = line.IndexOf(',');
                title = comma >= 0 ? line.Substring(comma + 1).Trim() : GetOr(attrs, "tvg-name", "Untitled");
                group = GetOr(attrs, "group-title", "Other");
                logo = GetOr(attrs, "tvg-logo", null);
            }
            else if (
                !line.StartsWith("#") &&
                Uri.TryCreate(line, UriKind.Absolute, out var u) &&
                (u.Scheme == "http" || u.Scheme == "https")
            )
            {
                var name = title ?? "Untitled";
                list.Add(new MediaEntry(name, group, logo, line, Classify(name, group, line)));
                title = null;
                logo = null;
                group = "Other";
            }
        }

        return list;
    }

    public static string Classify(string name, string group, string url)
    {
        var path = url.ToLowerInvariant().Split('?')[0];
        var t = name.ToLowerInvariant();
        var g = group.ToLowerInvariant();

        var ep = EpisodeRegexValue.IsMatch(name);
        if (path.Contains("/series/") || ep) return "series";
        if (path.Contains("/movie/") || path.Contains("/vod/")) return "movie";
        if (path.Contains("/live/") || path.EndsWith(".m3u8") || path.EndsWith(".ts")) return "live";
        if (new[] { ".mp4", ".mkv", ".avi", ".mov", ".m4v", ".webm" }.Any(path.EndsWith))
            return ep ? "series" : "movie";
        if (new[] { "series", "show", "مسلسل", "مسلسلات", "موسم", "حلقة" }.Any(x => g.Contains(x) || t.Contains(x)))
            return "series";
        if (new[] { "movie", "movies", "vod", "cinema", "film", "افلام", "أفلام", "فيلم" }.Any(x => g.Contains(x) || t.Contains(x)))
            return "movie";
        return "live";
    }

    private static string? GetOr(Dictionary<string, string> values, string key, string? fallback)
    {
        return values.TryGetValue(key, out var value) ? value : fallback;
    }
}
