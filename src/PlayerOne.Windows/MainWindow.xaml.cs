using PlayerOne.Windows.Models;
using PlayerOne.Windows.Services;

namespace PlayerOne.Windows;

public partial class MainWindow : System.Windows.Window
{
    readonly PlayerOneApi api = new();
    DeviceAuth? auth;

    public sealed record HomePoster(string Id,string Type,string Title,string Subtitle,string? Image,string? StreamUrl);

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_,__) => await InitializeAsync();
        Activated += (_,__) => { if (auth is not null && HomeView.Visibility == Visibility.Visible) RefreshHomeRail(); };
    }

    async Task InitializeAsync()
    {
        try
        {
            auth = SessionStore.Load() ?? await api.BootstrapAsync();
            SessionStore.Save(auth);
            DeviceIdText.Text = auth.DeviceId;
            DeviceKeyText.Text = auth.DeviceKey;
            StatusText.Text = "Device ready";
            SubText.Text = "Connected to Player One";
            ContinueButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Connection failed";
            SubText.Text = ex.Message;
        }
    }

    async void Continue_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (auth is null) return;
        BootstrapView.Visibility = Visibility.Collapsed;
        HomeView.Visibility = Visibility.Visible;
        await SyncPlaylistsAsync();
        RefreshHomeRail();
    }

    async Task SyncPlaylistsAsync()
    {
        if (auth is null) return;
        try
        {
            using var d = await api.GetPlaylistsAsync(auth);
            if (d.RootElement.TryGetProperty("playlists",out var a))
            {
                var ids = a.EnumerateArray()
                    .Select(p => p.TryGetProperty("id",out var id) ? id.GetInt64() : 0)
                    .Where(x => x > 0)
                    .ToList();
                var selected = PlaylistSelectionStore.Load();
                if (ids.Count > 0 && (!selected.HasValue || !ids.Contains(selected.Value))) PlaylistSelectionStore.Save(ids[0]);
            }
        }
        catch { }
    }

    void RefreshHomeRail()
    {
        if (auth is null) return;
        var playlist = PlaylistSelectionStore.Load();
        var posters = new List<HomePoster>();

        foreach (var r in PlayerStateStore.Recents(auth,playlist))
        {
            if (string.IsNullOrWhiteSpace(r.StreamUrl)) continue;
            posters.Add(new HomePoster($"recent-{posters.Count}",r.Type,r.Title,r.Subtitle,r.Image,r.StreamUrl));
            if (posters.Count >= 24) break;
        }

        if (posters.Count < 24)
        {
            foreach (var f in PlayerStateStore.Favorites(auth,playlist))
            {
                if (string.IsNullOrWhiteSpace(f.StreamUrl)) continue;
                if (posters.Any(x => x.StreamUrl == f.StreamUrl)) continue;
                posters.Add(new HomePoster(f.Id,f.Type,f.Title,f.Subtitle,f.Image,f.StreamUrl));
                if (posters.Count >= 24) break;
            }
        }

        HomeRail.ItemsSource = posters;
    }

    void HomePoster_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (auth is null || s is not System.Windows.Controls.Button b || b.Tag is not HomePoster p || string.IsNullOrWhiteSpace(p.StreamUrl)) return;
        var item = new XtreamItem(p.Id,p.Title,p.Subtitle,p.Image,p.StreamUrl,p.Type,0);
        new PlayerWindow(auth,item) { Owner = this }.Show();
    }

    void Settings_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (auth is null) return;
        new SettingsWindow(auth) { Owner = this }.ShowDialog();
        _ = SyncPlaylistsAsync();
    }

    void Library_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (auth is null) return;
        new LibraryWindow(auth) { Owner = this }.Show();
    }

    void Playlists_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (auth is null) return;
        new PlaylistWindow(auth) { Owner = this }.ShowDialog();
        _ = SyncPlaylistsAsync();
        RefreshHomeRail();
    }

    void Info_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (auth is null) return;
        new InfoWindow(auth) { Owner = this }.ShowDialog();
    }

    async void Refresh_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (auth is null) return;
        SubText.Text = "Refreshing…";
        await SyncPlaylistsAsync();
        RefreshHomeRail();
        SubText.Text = "Content refreshed";
    }

    void Live_Click(object s,System.Windows.RoutedEventArgs e)
    {
        if (auth is null) return;
        new LiveWindow(auth) { Owner = this }.Show();
    }

    void Movies_Click(object s,System.Windows.RoutedEventArgs e) => OpenCatalog("movie");
    void Series_Click(object s,System.Windows.RoutedEventArgs e) => OpenCatalog("series");

    void OpenCatalog(string kind)
    {
        if (auth is null) return;
        new CatalogWindow(auth,kind) { Owner = this }.Show();
    }

    void Exit_Click(object s,System.Windows.RoutedEventArgs e) => ConfirmExit();

    void Window_KeyDown(object s,System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape && HomeView.Visibility == Visibility.Visible)
        {
            ConfirmExit();
            e.Handled = true;
        }
    }

    void ConfirmExit()
    {
        if (!PlayerSettings.Load().ConfirmExit)
        {
            System.Windows.Application.Current.Shutdown();
            return;
        }
        var result = System.Windows.MessageBox.Show("Do you want to exit the app?","Exit Player One",System.Windows.MessageBoxButton.YesNo,System.Windows.MessageBoxImage.Question);
        if (result == System.Windows.MessageBoxResult.Yes) System.Windows.Application.Current.Shutdown();
    }
}
