using System.Text.Json;
using PlayerOne.Windows.Models;
using PlayerOne.Windows.Services;

namespace PlayerOne.Windows;

public partial class CatalogWindow : System.Windows.Window
{
    readonly DeviceAuth auth;
    readonly string kind;
    readonly PlayerOneApi api = new();
    List<XtreamItem> all = [];
    string? host,user,pass;
    CancellationTokenSource? filterCts;
    bool binding;

    public sealed record CatalogRow(IReadOnlyList<XtreamItem> Items);

    public CatalogWindow(DeviceAuth a,string k)
    {
        InitializeComponent();
        auth = a;
        kind = k;
        TitleText.Text = k == "live" ? "LIVE" : k == "movie" ? "MOVIES" : "SERIES";
        Loaded += async (_,__) => await LoadAsync();
        Closed += (_,__) => filterCts?.Cancel();
    }

    async Task LoadAsync()
    {
        try
        {
            using var d = await api.GetPlaylistsAsync(auth);
            if (!d.RootElement.TryGetProperty("playlists",out var arr) || arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0)
            {
                MessageBox.Show("No playlists linked to this device.","Player One");
                return;
            }

            var selected = PlaylistSelectionStore.Load();
            JsonElement p = arr[0];
            foreach (var x in arr.EnumerateArray())
                if (x.TryGetProperty("id",out var xid) && selected == xid.GetInt64()) { p = x; break; }

            var id = p.GetProperty("id").GetInt64();
            PlaylistSelectionStore.Save(id);
            using var cfg = await api.GetPlaylistConfigAsync(auth,id);
            var q = cfg.RootElement.GetProperty("playlist");
            var type = q.TryGetProperty("type",out var t) ? t.ToString().ToLowerInvariant() : "";

            if (type == "xtream")
            {
                host = q.GetProperty("host").ToString();
                user = q.GetProperty("username").ToString();
                pass = q.GetProperty("password").ToString();
                all = await new XtreamClient().LoadAsync(host,user,pass,kind);
            }
            else if (type == "m3u" && q.TryGetProperty("url",out var u))
            {
                all = await new M3uClient().LoadAsync(u.ToString(),kind);
            }

            await BindAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message,"Player One");
        }
    }

    async Task BindAsync()
    {
        var providerGroups = await Task.Run(() =>
            all.Select((item,index) => new { Item = item, Index = index, Group = CleanGroup(item.Group) })
               .GroupBy(x => x.Group,StringComparer.OrdinalIgnoreCase)
               .Select(g => new { Name = g.Key, Order = g.Min(x => x.Item.GroupOrder), First = g.Min(x => x.Index) })
               .OrderBy(x => x.Order)
               .ThenBy(x => x.First)
               .Select(x => x.Name)
               .ToList());

        binding = true;
        var specials = kind == "live"
            ? new[] { "All","Favorites" }
            : new[] { "All","Recently added","Continue watching","Favorites" };
        Groups.ItemsSource = specials.Concat(providerGroups).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Groups.SelectedIndex = 0;
        binding = false;
        await FilterAsync(false);
    }

    async Task FilterAsync(bool debounceSearch)
    {
        if (binding) return;
        filterCts?.Cancel();
        var cts = new CancellationTokenSource();
        filterCts = cts;
        var token = cts.Token;
        var group = Groups.SelectedItem?.ToString() ?? "All";
        var query = SearchBox.Text.Trim();

        try
        {
            if (debounceSearch && query.Length > 0) await Task.Delay(140,token);

            var list = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                IEnumerable<XtreamItem> src = all;

                if (query.Length > 0)
                {
                    src = all.Where(x => x.Name.Contains(query,StringComparison.OrdinalIgnoreCase) || CleanGroup(x.Group).Contains(query,StringComparison.OrdinalIgnoreCase));
                }
                else if (group == "Favorites")
                {
                    var favorites = PlayerStateStore.Favorites(auth,PlaylistSelectionStore.Load())
                        .Where(x => x.Type == kind || (kind == "live" && x.Type == "live"))
                        .ToList();
                    src = all.Where(x => favorites.Any(v => v.Id == x.Id || (!string.IsNullOrWhiteSpace(v.StreamUrl) && v.StreamUrl == x.StreamUrl)));
                }
                else if (group == "Continue watching")
                {
                    var recents = PlayerStateStore.Recents(auth,PlaylistSelectionStore.Load()).Where(PlayerStateStore.Meaningful).ToList();
                    src = all.Where(x => recents.Any(v => (!string.IsNullOrWhiteSpace(v.StreamUrl) && v.StreamUrl == x.StreamUrl) || Norm(v.Title) == Norm(x.Name)));
                }
                else if (group == "Recently added")
                {
                    src = all.Any(x => x.AddedAt > 0)
                        ? all.OrderByDescending(x => x.AddedAt).Take(60)
                        : all.Take(60);
                }
                else if (group != "All")
                {
                    src = all.Where(x => string.Equals(CleanGroup(x.Group),group,StringComparison.OrdinalIgnoreCase));
                }

                token.ThrowIfCancellationRequested();
                return src.ToList();
            },token);

            token.ThrowIfCancellationRequested();
            Items.ItemsSource = BuildRows(list,6);
            CountText.Text = $"{list.Count:N0} items";
        }
        catch (OperationCanceledException) { }
    }

    static List<CatalogRow> BuildRows(List<XtreamItem> items,int columns)
    {
        var rows = new List<CatalogRow>((items.Count + columns - 1) / columns);
        for (var i = 0; i < items.Count; i += columns)
            rows.Add(new CatalogRow(items.GetRange(i,Math.Min(columns,items.Count - i))));
        return rows;
    }

    static string CleanGroup(string? value) => string.IsNullOrWhiteSpace(value) ? "Other" : value.Trim();
    static string Norm(string? v) => new string((v ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    async void Group_Changed(object s,System.Windows.Controls.SelectionChangedEventArgs e) => await FilterAsync(false);
    async void Search_Changed(object s,System.Windows.Controls.TextChangedEventArgs e) => await FilterAsync(true);
    void Back_Click(object s,System.Windows.RoutedEventArgs e) => Close();

    void Window_KeyDown(object s,System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    async void ToggleFavorite(XtreamItem x)
    {
        PlayerStateStore.ToggleFavorite(auth,PlaylistSelectionStore.Load(),new(x.Kind,x.Id,x.Name,x.Group,x.Image,x.StreamUrl,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        if (Groups.SelectedItem?.ToString() == "Favorites") await FilterAsync(false);
    }

    void Card_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (s is System.Windows.Controls.Button b && b.Tag is XtreamItem x) OpenItem(x);
    }

    void Card_KeyDown(object s,System.Windows.Input.KeyEventArgs e)
    {
        if (s is not System.Windows.Controls.Button b || b.Tag is not XtreamItem x) return;
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            OpenItem(x);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.F)
        {
            ToggleFavorite(x);
            e.Handled = true;
        }
    }

    void OpenItem(XtreamItem x)
    {
        if (kind == "series" && x.StreamUrl is null && host is not null && user is not null && pass is not null)
        {
            new SeriesWindow(auth,x,host,user,pass) { Owner = this }.Show();
        }
        else if (kind == "movie" && x.StreamUrl is not null && host is not null && user is not null && pass is not null)
        {
            new MovieDetailsWindow(auth,x,host,user,pass) { Owner = this }.Show();
        }
        else if (x.StreamUrl is not null)
        {
            var queue = kind == "live" ? all : new List<XtreamItem> { x };
            new PlayerWindow(auth,x,queue) { Owner = this }.Show();
        }
    }
}
