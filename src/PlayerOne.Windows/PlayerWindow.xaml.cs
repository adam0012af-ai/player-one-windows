using LibVLCSharp.Shared;
using PlayerOne.Windows.Models;
using PlayerOne.Windows.Services;
using System.Windows.Threading;

namespace PlayerOne.Windows;

public partial class PlayerWindow : System.Windows.Window
{
    readonly DeviceAuth auth;
    XtreamItem item;
    readonly List<XtreamItem> queue;
    int queueIndex;
    readonly LibVLC lib;
    readonly MediaPlayer mp;
    Media? media;
    readonly DispatcherTimer overlayTimer;
    bool compact;
    bool fullscreen;
    int aspect;
    int liveCandidate;
    int reconnectAttempts;
    List<string> candidates = [];
    Rect normalBounds;
    WindowStyle normalStyle;
    ResizeMode normalResize;
    bool normalShowInTaskbar;

    public PlayerWindow(DeviceAuth a,XtreamItem i,List<XtreamItem>? q=null)
    {
        InitializeComponent();
        Core.Initialize();
        auth = a;
        item = i;
        queue = q ?? new() { i };
        queueIndex = Math.Max(0,queue.FindIndex(x => x.Id == i.Id));

        var adv = AdvancedSettings.Load();
        var cache = adv.BufferProfile switch { "fast" => "650", "stable" => "2200", _ => "1200" };
        lib = new LibVLC("--network-caching=" + cache,"--file-caching=" + cache,"--no-video-title-show");
        mp = new MediaPlayer(lib);
        VideoView.MediaPlayer = mp;
        mp.Playing += (_,__) => Dispatcher.Invoke(OnPlaying);
        mp.EndReached += (_,__) => Dispatcher.Invoke(OnEnded);
        mp.EncounteredError += (_,__) => Dispatcher.Invoke(OnError);

        overlayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        overlayTimer.Tick += (_,__) => { overlayTimer.Stop(); if (!compact) ControlOverlay.Visibility = Visibility.Collapsed; };

        NowPlaying.Text = i.Name;
        NowGroup.Text = i.Group;

        Loaded += (_,__) =>
        {
            normalStyle = WindowStyle;
            normalResize = ResizeMode;
            normalShowInTaskbar = ShowInTaskbar;
            normalBounds = new Rect(Left,Top,ActualWidth > 0 ? ActualWidth : Width,ActualHeight > 0 ? ActualHeight : Height);
            ApplySettings();
            FavoriteButton.Visibility = item.Kind == "live" ? Visibility.Visible : Visibility.Collapsed;
            PrevButton.Visibility = item.Kind == "live" ? Visibility.Visible : Visibility.Collapsed;
            NextButton.Visibility = item.Kind == "live" ? Visibility.Visible : Visibility.Collapsed;
            if (item.Kind == "live") FavoriteButton.Content = PlayerStateStore.IsFavorite(auth,PlaylistSelectionStore.Load(),"live",item.Id) ? "♥" : "♡";
            ShowOverlay();
            Play();
            Focus();
        };

        Closing += (_,__) =>
        {
            overlayTimer.Stop();
            Save();
            mp.Stop();
            media?.Dispose();
            mp.Dispose();
            lib.Dispose();
        };
    }

    void Play()
    {
        if (string.IsNullOrWhiteSpace(item.StreamUrl)) return;
        var adv = AdvancedSettings.Load();
        candidates = (item.Kind == "live" ? LivePlayback.Candidates(item.StreamUrl) : new[] { item.StreamUrl }).Distinct().ToList();
        if (item.Kind == "live" && adv.StreamingFormat != "auto")
        {
            var wanted = adv.StreamingFormat == "hls" ? ".m3u8" : ".ts";
            candidates = candidates.OrderByDescending(x => x.IndexOf(wanted,StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }
        liveCandidate = 0;
        PlayCandidate();
    }

    void PlayCandidate()
    {
        if (liveCandidate >= candidates.Count)
        {
            PlayerStatus.Text = "Playback failed";
            ShowOverlay();
            return;
        }
        media?.Dispose();
        media = new Media(lib,new Uri(candidates[liveCandidate]));
        PlayerStatus.Text = "Loading…";
        mp.Play(media);
    }

    void Close_Click(object s,RoutedEventArgs e) => ReturnToPrevious();

    void ReturnToPrevious()
    {
        var owner = Owner;
        Close();
        if (owner is not null)
        {
            owner.Show();
            owner.Activate();
            owner.Focus();
        }
    }

    void OnPlaying()
    {
        PlayerStatus.Text = "";
        reconnectAttempts = 0;
        if (item.Kind == "live")
            PlayerStateStore.AddRecent(auth,PlaylistSelectionStore.Load(),new("live",item.Name,item.Group,item.Image,item.StreamUrl,0,0,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        else
        {
            var r = PlayerStateStore.Recents(auth,PlaylistSelectionStore.Load()).FirstOrDefault(x => x.StreamUrl == item.StreamUrl && PlayerStateStore.Meaningful(x));
            if (r is not null && r.PositionMs > 0 && mp.Time < 3000) mp.Time = r.PositionMs;
        }
        ShowOverlay();
    }

    async void OnError()
    {
        if (item.Kind == "live" && PlayerSettings.Load().AutoReconnect)
        {
            if (++liveCandidate < candidates.Count)
            {
                await Task.Delay(900);
                PlayCandidate();
                return;
            }
            if (reconnectAttempts < 2)
            {
                reconnectAttempts++;
                await Task.Delay(900 * reconnectAttempts);
                liveCandidate = 0;
                PlayCandidate();
                return;
            }
        }
        PlayerStatus.Text = "Playback error";
        ShowOverlay();
    }

    void OnEnded()
    {
        Save();
        if (item.Kind == "series" && AdvancedSettings.Load().AutoNext && queueIndex + 1 < queue.Count)
        {
            item = queue[++queueIndex];
            NowPlaying.Text = item.Name;
            NowGroup.Text = item.Group;
            Play();
        }
    }

    void ApplySettings()
    {
        var s = PlayerSettings.Load();
        mp.AspectRatio = s.DefaultAspect switch { "fill" => "16:9", "zoom" => "4:3", _ => null };
    }

    void CycleAudio()
    {
        var tracks = mp.AudioTrackDescription?.ToList();
        if (tracks is null || tracks.Count < 2) { PlayerStatus.Text = "No alternate audio track"; ShowOverlay(); return; }
        var ids = tracks.Select(x => x.Id).ToList();
        var i = ids.IndexOf(mp.AudioTrack);
        mp.SetAudioTrack(ids[(i + 1 + ids.Count) % ids.Count]);
        PlayerStatus.Text = "Audio track changed";
        ShowOverlay();
    }

    void CycleSubtitle()
    {
        var s = PlayerSettings.Load();
        if (!s.SubtitlesEnabled) { mp.SetSpu(-1); PlayerStatus.Text = "Subtitles off"; ShowOverlay(); return; }
        var tracks = mp.SpuDescription?.ToList();
        if (tracks is null || tracks.Count == 0) { PlayerStatus.Text = "No subtitles"; ShowOverlay(); return; }
        var ids = tracks.Select(x => x.Id).ToList();
        var i = ids.IndexOf(mp.Spu);
        mp.SetSpu(ids[(i + 1 + ids.Count) % ids.Count]);
        PlayerStatus.Text = "Subtitle track changed";
        ShowOverlay();
    }

    void Prev_Click(object s,RoutedEventArgs e) { if (item.Kind == "live") SwitchLive(-1); }
    void Next_Click(object s,RoutedEventArgs e) { if (item.Kind == "live") SwitchLive(1); }
    void Favorite_Click(object s,RoutedEventArgs e) { if (item.Kind == "live") ToggleLiveFavorite(); }
    void Pause_Click(object s,RoutedEventArgs e) { if (mp.IsPlaying) mp.Pause(); else mp.Play(); ShowOverlay(); }
    void Back_Click(object s,RoutedEventArgs e) => Seek(-10000);
    void Forward_Click(object s,RoutedEventArgs e) => Seek(10000);

    void Seek(long delta)
    {
        if (item.Kind != "live" && mp.Length > 0) mp.Time = Math.Min(mp.Length,Math.Max(0,mp.Time + delta));
        ShowOverlay();
    }

    void Audio_Click(object s,RoutedEventArgs e) => CycleAudio();
    void Subs_Click(object s,RoutedEventArgs e) => CycleSubtitle();

    void Aspect_Click(object s,RoutedEventArgs e)
    {
        aspect = (aspect + 1) % 3;
        mp.AspectRatio = aspect switch { 0 => null, 1 => "16:9", _ => "4:3" };
        PlayerStatus.Text = aspect switch { 0 => "Auto aspect", 1 => "16:9", _ => "4:3" };
        ShowOverlay();
    }

    void Compact_Click(object s,RoutedEventArgs e) => ToggleCompact();
    void Full_Click(object s,RoutedEventArgs e) => ToggleFull();

    void ToggleFull()
    {
        if (compact) RestoreFromCompact();

        if (!fullscreen)
        {
            SaveNormalWindowState();
            fullscreen = true;
            WindowState = WindowState.Normal;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            ShowOverlay();
        }
        else
        {
            fullscreen = false;
            WindowState = WindowState.Normal;
            WindowStyle = normalStyle;
            ResizeMode = normalResize;
            RestoreNormalBounds();
            ShowOverlay();
        }
    }

    void ToggleCompact()
    {
        if (compact) { RestoreFromCompact(); ShowOverlay(); return; }
        if (fullscreen) ToggleFull();

        SaveNormalWindowState();
        compact = true;
        fullscreen = false;
        WindowState = WindowState.Normal;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Width = 480;
        Height = 270;
        Left = Math.Max(SystemParameters.WorkArea.Left,SystemParameters.WorkArea.Right - Width - 18);
        Top = Math.Max(SystemParameters.WorkArea.Top,SystemParameters.WorkArea.Bottom - Height - 18);
        ControlOverlay.Visibility = Visibility.Collapsed;
        overlayTimer.Stop();
    }

    void RestoreFromCompact()
    {
        if (!compact) return;
        compact = false;
        Topmost = false;
        ShowInTaskbar = normalShowInTaskbar;
        WindowState = WindowState.Normal;
        WindowStyle = normalStyle;
        ResizeMode = normalResize;
        RestoreNormalBounds();
    }

    void SaveNormalWindowState()
    {
        if (!compact && !fullscreen)
        {
            normalStyle = WindowStyle;
            normalResize = ResizeMode;
            normalShowInTaskbar = ShowInTaskbar;
            if (WindowState == WindowState.Normal)
                normalBounds = new Rect(Left,Top,ActualWidth > 0 ? ActualWidth : Width,ActualHeight > 0 ? ActualHeight : Height);
        }
    }

    void RestoreNormalBounds()
    {
        if (normalBounds.Width <= 0 || normalBounds.Height <= 0) return;
        Left = normalBounds.Left;
        Top = normalBounds.Top;
        Width = normalBounds.Width;
        Height = normalBounds.Height;
    }

    void ShowOverlay()
    {
        if (compact) return;
        ControlOverlay.Visibility = Visibility.Visible;
        overlayTimer.Stop();
        overlayTimer.Start();
    }

    void ToggleOverlay()
    {
        if (compact) return;
        if (ControlOverlay.Visibility == Visibility.Visible)
        {
            overlayTimer.Stop();
            ControlOverlay.Visibility = Visibility.Collapsed;
        }
        else ShowOverlay();
    }

    void Video_MouseMove(object s,System.Windows.Input.MouseEventArgs e)
    {
        if (!compact) ShowOverlay();
    }

    void Video_PreviewMouseLeftButtonUp(object s,System.Windows.Input.MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as DependencyObject)) return;
        if (compact)
        {
            RestoreFromCompact();
            ShowOverlay();
        }
        else ToggleOverlay();
        e.Handled = true;
    }

    static bool IsInsideButton(DependencyObject? source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is System.Windows.Controls.Button) return true;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    void Window_KeyDown(object s,System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            if (compact) { RestoreFromCompact(); ShowOverlay(); }
            else if (fullscreen) ToggleFull();
            else ReturnToPrevious();
            e.Handled = true;
            return;
        }

        if (e.Key == System.Windows.Input.Key.Space) Pause_Click(s,e);
        else if (e.Key == System.Windows.Input.Key.F11) ToggleFull();
        else if (e.Key == System.Windows.Input.Key.P) ToggleCompact();
        else if (e.Key == System.Windows.Input.Key.A) CycleAudio();
        else if (e.Key == System.Windows.Input.Key.S) CycleSubtitle();
        else if (e.Key == System.Windows.Input.Key.Left) Seek(-10000);
        else if (e.Key == System.Windows.Input.Key.Right) Seek(10000);
        else if (item.Kind == "live" && e.Key == System.Windows.Input.Key.Down) SwitchLive(1);
        else if (item.Kind == "live" && e.Key == System.Windows.Input.Key.Up) SwitchLive(-1);
        else if (item.Kind == "live" && e.Key == System.Windows.Input.Key.F) ToggleLiveFavorite();
    }

    void ToggleLiveFavorite()
    {
        var on = PlayerStateStore.ToggleFavorite(auth,PlaylistSelectionStore.Load(),new("live",item.Id,item.Name,item.Group,item.Image,item.StreamUrl,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        FavoriteButton.Content = on ? "♥" : "♡";
        PlayerStatus.Text = on ? "Added to favorites" : "Removed from favorites";
        ShowOverlay();
    }

    void SwitchLive(int delta)
    {
        if (queue.Count < 2) return;
        Save();
        queueIndex = (queueIndex + delta + queue.Count) % queue.Count;
        item = queue[queueIndex];
        NowPlaying.Text = item.Name;
        NowGroup.Text = item.Group;
        FavoriteButton.Content = PlayerStateStore.IsFavorite(auth,PlaylistSelectionStore.Load(),"live",item.Id) ? "♥" : "♡";
        Play();
        ShowOverlay();
    }

    void Save()
    {
        if (item.Kind == "live" || string.IsNullOrWhiteSpace(item.StreamUrl)) return;
        PlayerStateStore.AddRecent(auth,PlaylistSelectionStore.Load(),new(item.Kind,item.Name,item.Group,item.Image,item.StreamUrl,Math.Max(0,mp.Time),Math.Max(0,mp.Length),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
    }
}
