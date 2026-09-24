using PlayerOne.Windows.Models;
using PlayerOne.Windows.Services;
namespace PlayerOne.Windows;
public partial class MainWindow : System.Windows.Window {
 readonly PlayerOneApi api=new(); DeviceAuth? auth;
 public MainWindow(){InitializeComponent();Loaded+=async(_,__)=>await InitializeAsync();Activated+=async(_,__)=>await RefreshHomeAsync();}
 async Task InitializeAsync(){try{auth=SessionStore.Load()??await api.BootstrapAsync();SessionStore.Save(auth);var snapshot=await api.GetDeviceSnapshotAsync(auth);DeviceIdText.Text=auth.DeviceId;DeviceKeyText.Text=auth.DeviceKey;StatusText.Text="Device ready";SubText.Text=StatusLine(snapshot);ContinueButton.IsEnabled=true;}catch(Exception ex){StatusText.Text="Connection failed";SubText.Text=ex.Message;}}
 async Task RefreshHomeAsync(){if(auth is null||HomeView.Visibility!=System.Windows.Visibility.Visible)return;try{var x=await api.GetDeviceSnapshotAsync(auth);HomeInfo.Text=$"{x.PlaylistCount} playlist(s) linked • {ActivationLine(x)} • Windows PC";}catch{}}
 static string ActivationLine(DeviceSnapshot x){if(string.Equals(x.PlanCode,"LIFETIME",StringComparison.OrdinalIgnoreCase))return "Lifetime";var end=x.Status.Equals("trial",StringComparison.OrdinalIgnoreCase)?x.TrialExpiresAt:x.ExpiresAt;return string.IsNullOrWhiteSpace(end)?x.Status:$"{x.Status} • {end.Split('T')[0]}";}
 static string StatusLine(DeviceSnapshot x)=>$"Player One • {ActivationLine(x)} • {x.PlaylistCount} playlist(s)";
 async void Continue_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;try{BootstrapView.Visibility=System.Windows.Visibility.Collapsed;HomeView.Visibility=System.Windows.Visibility.Visible;await RefreshHomeAsync();}catch(Exception ex){SubText.Text=ex.Message;}}
 void Library_Click(object s,System.Windows.RoutedEventArgs e){if(auth is not null)new LibraryWindow(auth).Show();}
 void Playlists_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;var w=new PlaylistWindow(auth);w.ShowDialog();_=RefreshHomeAsync();}
 void Info_Click(object s,System.Windows.RoutedEventArgs e){if(auth is not null)new InfoWindow(auth).ShowDialog();}
 void Settings_Click(object s,System.Windows.RoutedEventArgs e){new SettingsWindow().ShowDialog();}
 void Live_Click(object s,System.Windows.RoutedEventArgs e)=>OpenCatalog("live");
 void Movies_Click(object s,System.Windows.RoutedEventArgs e)=>OpenCatalog("movie");
 void Series_Click(object s,System.Windows.RoutedEventArgs e)=>OpenCatalog("series");
 void OpenCatalog(string kind){if(auth is null)return;new CatalogWindow(auth,kind).Show();}
}