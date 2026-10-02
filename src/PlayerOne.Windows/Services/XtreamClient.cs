using System.Text.Json;
namespace PlayerOne.Windows.Services;

public sealed record XtreamItem(
    string Id,
    string Name,
    string Group,
    string? Image,
    string? StreamUrl,
    string Kind,
    long AddedAt,
    int GroupOrder = int.MaxValue);

public sealed record MovieInfo(string? Plot,string? Genre,string? Director,string? Cast,string? Rating,string? Year,string? Duration,string? Backdrop,string? Trailer);

public sealed class XtreamClient
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(90) };

    static string Enc(string x) => Uri.EscapeDataString(x);
    static string Api(string h,string u,string p) => $"{h.TrimEnd('/')}/player_api.php?username={Enc(u)}&password={Enc(p)}";

    public async Task<List<XtreamItem>> LoadAsync(string host,string user,string pass,string kind)
    {
        var baseUrl = Api(host,user,pass);
        var catAction = kind == "live" ? "get_live_categories" : kind == "movie" ? "get_vod_categories" : "get_series_categories";
        var itemAction = kind == "live" ? "get_live_streams" : kind == "movie" ? "get_vod_streams" : "get_series";

        // Match Android: fetch categories and content together, and never resume heavy JSON work on the UI thread.
        var categoriesTask = Http.GetStringAsync($"{baseUrl}&action={catAction}");
        var itemsTask = Http.GetStringAsync($"{baseUrl}&action={itemAction}");
        await Task.WhenAll(categoriesTask, itemsTask).ConfigureAwait(false);

        using var catsDoc = JsonDocument.Parse(await categoriesTask.ConfigureAwait(false));
        using var itemsDoc = JsonDocument.Parse(await itemsTask.ConfigureAwait(false));

        var categoryMap = new Dictionary<string,(string Name,int Order)>(StringComparer.OrdinalIgnoreCase);
        var order = 0;
        foreach (var c in catsDoc.RootElement.EnumerateArray())
        {
            var id = Get(c,"category_id");
            if (id.Length == 0) continue;
            categoryMap[id] = (Get(c,"category_name", kind == "live" ? "Live" : kind == "movie" ? "Movies" : "Series"), order++);
        }

        var items = itemsDoc.RootElement;
        var result = new List<XtreamItem>(items.ValueKind == JsonValueKind.Array ? items.GetArrayLength() : 0);
        if (items.ValueKind != JsonValueKind.Array) return result;

        foreach (var x in items.EnumerateArray())
        {
            var id = Get(x,kind == "series" ? "series_id" : "stream_id");
            if (id.Length == 0) continue;

            var categoryId = Get(x,"category_id");
            var category = categoryMap.TryGetValue(categoryId, out var mapped)
                ? mapped
                : (kind == "live" ? "Live" : kind == "movie" ? "Movies" : "Series", int.MaxValue);

            string? stream = null;
            if (kind != "series")
            {
                var ext = kind == "movie" ? Get(x,"container_extension","mp4") : "ts";
                stream = $"{host.TrimEnd('/')}/{(kind == "movie" ? "movie" : "live")}/{Enc(user)}/{Enc(pass)}/{id}.{ext}";
            }

            result.Add(new XtreamItem(
                id,
                Get(x,"name",kind),
                category.Item1,
                GetN(x,kind == "series" ? "cover" : "stream_icon"),
                stream,
                kind,
                Long(x,"added"),
                category.Item2));
        }

        return result.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
    }

    public async Task<MovieInfo> MovieInfoAsync(string h,string u,string p,string id)
    {
        var json = await Http.GetStringAsync($"{Api(h,u,p)}&action=get_vod_info&vod_id={Enc(id)}").ConfigureAwait(false);
        using var d = JsonDocument.Parse(json);
        var root = d.RootElement;
        var i = root.TryGetProperty("info",out var z) ? z : root;
        return new(GetN(i,"plot"),GetN(i,"genre"),GetN(i,"director"),GetN(i,"cast"),GetN(i,"rating"),GetN(i,"releasedate")??GetN(i,"year"),GetN(i,"duration"),GetN(i,"movie_image")??GetN(i,"backdrop_path"),GetN(i,"youtube_trailer"));
    }

    static string Get(JsonElement e,string n,string fallback="") => e.TryGetProperty(n,out var v) ? v.ToString() : fallback;
    static string? GetN(JsonElement e,string n) => e.TryGetProperty(n,out var v) && !string.IsNullOrWhiteSpace(v.ToString()) ? v.ToString() : null;
    static long Long(JsonElement e,string n) => e.TryGetProperty(n,out var v) && long.TryParse(v.ToString(),out var x) ? x : 0;
}
