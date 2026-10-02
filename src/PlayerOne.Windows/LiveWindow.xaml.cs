using System.Text.Json;
using PlayerOne.Windows.Models;
using PlayerOne.Windows.Services;

namespace PlayerOne.Windows;

public partial class LiveWindow : System.Windows.Window
{
    readonly DeviceAuth auth;
    readonly PlayerOneApi api = new();
    List<XtreamItem> all = [];
    List<XtreamItem> visible = [];
    CancellationTokenSource? filterCts;
    bool binding;

    public sealed record Row(string Number,XtreamItem Item);

    public LiveWindow(DeviceAuth a)
    {
        InitializeComponent();
        auth = a;
        Loaded += async (_,__) => await LoadAsync();
        Closed += (_,__) => filterCts?.Cancel();
    }

    async Task LoadAsync()
    {
        try
        {
            using var d = await api.GetPlaylistsAsync(auth);
            if (!d.RootElement.TryGetProperty("playlists",out var arr) || arr.GetArrayLength() == 0) return;

            var selected = PlaylistSelectionStore.Load();
            JsonElement p = arr[0];
            foreach (var x in arr.EnumerateArray())
                if (x.TryGetProperty("id",out var id) && id.GetInt64() == selected) { p = x; break; }

            var pid = p.GetProperty("id").GetInt64();
            PlaylistSelectionStore.Save(pid);
            using var cfg = await api.GetPlaylistConfigAsync(auth,pid);
            var q = cfg.RootElement.GetProperty("playlist");
            var type = q.TryGetProperty("type",out var t) ? t.ToString().ToLowerInvariant() : "";

            if (type == "xtream")
                all = await new XtreamClient().LoadAsync(q.GetProperty("host").ToString(),q.GetProperty("username").ToString(),q.GetProperty("password").ToString(),"live");
            else if (type == "m3u" && q.TryGetProperty("url",out var u))
                all = await new M3uClient().LoadAsync(u.ToString(),"live");

            await BindAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message,"Player One");
        }
    }

    async Task BindAsync()
    {
        var groups = await Task.Run(() =>
            all.Select((item,index) => new { Item = item, Index = index, Group = CleanGroup(item.Group) })
               .GroupBy(x => x.Group,StringComparer.OrdinalIgnoreCase)
               .Select(g => new { Name = g.Key, Order = g.Min(x => x.Item.GroupOrder), First = g.Min(x => x.Index) })
               .OrderBy(x => x.Order)
               .ThenBy(x => x.First)
               .Select(x => x.Name)
               .ToList());

        binding = true;
        Groups.ItemsSource = new[] { "All","Favorites" }.Concat(groups).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
            if (debounceSearch && query.Length > 0) await Task.Delay(120,token);

            var filtered = await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                IEnumerable<XtreamItem> src = all;

                if (query.Length > 0)
                {
                    src = all.Where(x => x.Name.Contains(query,StringComparison.OrdinalIgnoreCase) || CleanGroup(x.Group).Contains(query,StringComparison.OrdinalIgnoreCase));
                }
                else if (group == "Favorites")
                {
                    var favorites = PlayerStateStore.Favorites(auth,PlaylistSelectionStore.Load()).Where(x => x.Type == "live").ToList();
                    src = all.Where(x => favorites.Any(v => v.Id == x.Id || v.StreamUrl == x.StreamUrl));
                }
                else if (group != "All")
                {
                    src = all.Where(x => string.Equals(CleanGroup(x.Group),group,StringComparison.OrdinalIgnoreCase));
                }

                return src.ToList();
            },token);

            token.ThrowIfCancellationRequested();
            visible = filtered;
            Channels.ItemsSource = visible.Select((x,i) => new Row((i + 1).ToString("000"),x)).ToList();
            CountText.Text = $"{visible.Count:N0} channels";
            if (Channels.Items.Count > 0) Channels.SelectedIndex = 0;
        }
        catch (OperationCanceledException) { }
    }

    static string CleanGroup(string? value) => string.IsNullOrWhiteSpace(value) ? "أخرى" : value.Trim();

    async void Group_Changed(object s,System.Windows.Controls.SelectionChangedEventArgs e) => await FilterAsync(false);
    async void Search_Changed(object s,System.Windows.Controls.TextChangedEventArgs e) => await FilterAsync(true);

    void Channel_Changed(object s,System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (Channels.SelectedItem is not Row r) return;
        PreviewName.Text = r.Item.Name;
        PreviewGroup.Text = r.Item.Group;
        PreviewImage.Source = null;
        if (Uri.TryCreate(r.Item.Image,UriKind.Absolute,out var uri))
        {
            try
            {
                var image = new System.Windows.Media.Imaging.BitmapImage();
                image.BeginInit();
                image.UriSource = uri;
                image.DecodePixelWidth = 420;
                image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                image.EndInit();
                PreviewImage.Source = image;
            }
            catch { }
        }
    }

    void Play_Click(object s,System.Windows.RoutedEventArgs e) => Play();
    void Channel_DoubleClick(object s,System.Windows.Input.MouseButtonEventArgs e) => Play();

    void Play()
    {
        if (Channels.SelectedItem is not Row r || r.Item.StreamUrl is null) return;
        new PlayerWindow(auth,r.Item,visible) { Owner = this }.Show();
    }

    async void Favorite_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (Channels.SelectedItem is not Row r) return;
        PlayerStateStore.ToggleFavorite(auth,PlaylistSelectionStore.Load(),new("live",r.Item.Id,r.Item.Name,r.Item.Group,r.Item.Image,r.Item.StreamUrl,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        if (Groups.SelectedItem?.ToString() == "Favorites") await FilterAsync(false);
    }

    void Back_Click(object s,System.Windows.RoutedEventArgs e) => Close();

    void Window_KeyDown(object s,System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter) Play();
        else if (e.Key == System.Windows.Input.Key.F && Channels.SelectedItem is Row) Favorite_Click(s,e);
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }
}
