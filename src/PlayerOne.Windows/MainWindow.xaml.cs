using PlayerOne.Windows.Models;
using PlayerOne.Windows.Services;
namespace PlayerOne.Windows;
public partial class MainWindow : System.Windows.Window {
 readonly PlayerOneApi api=new(); DeviceAuth? auth;
 public MainWindow(){InitializeComponent();Loaded+=async(_,__)=>await InitializeAsync();}
 async Task InitializeAsync(){try{auth=SessionStore.Load()??await api.BootstrapAsync();SessionStore.Save(auth);using var status=await api.GetStatusAsync(auth);DeviceIdText.Text=auth.DeviceId;DeviceKeyText.Text=auth.DeviceKey;StatusText.Text="Device ready";SubText.Text="Connected to Player One.";ContinueButton.IsEnabled=true;}catch(Exception ex){StatusText.Text="Connection failed";SubText.Text=ex.Message;}}
 async void Continue_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;try{using var d=await api.GetPlaylistsAsync(auth);var n=d.RootElement.TryGetProperty("playlists",out var p)&&p.ValueKind==System.Text.Json.JsonValueKind.Array?p.GetArrayLength():0;BootstrapView.Visibility=System.Windows.Visibility.Collapsed;HomeView.Visibility=System.Windows.Visibility.Visible;HomeInfo.Text=$"{n} playlist(s) linked • {DeviceIdentity.Label()}";}catch(Exception ex){SubText.Text=ex.Message;}}
 void Library_Click(object s,System.Windows.RoutedEventArgs e){if(auth is not null)new LibraryWindow(auth).Show();}\n void Playlists_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;new PlaylistWindow(auth).ShowDialog();}
 void Info_Click(object s,System.Windows.RoutedEventArgs e){if(auth is not null)new InfoWindow(auth).ShowDialog();}\n void Settings_Click(object s,System.Windows.RoutedEventArgs e){new SettingsWindow().ShowDialog();}
 void Live_Click(object s,System.Windows.RoutedEventArgs e)=>OpenCatalog("live");
 void Movies_Click(object s,System.Windows.RoutedEventArgs e)=>OpenCatalog("movie");
 void Series_Click(object s,System.Windows.RoutedEventArgs e)=>OpenCatalog("series");
 void OpenCatalog(string kind){if(auth is null)return;new CatalogWindow(auth,kind).Show();}
}