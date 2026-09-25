using PlayerOne.Windows.Models;
using PlayerOne.Windows.Services;
namespace PlayerOne.Windows;
public partial class MainWindow : System.Windows.Window {
 readonly PlayerOneApi api=new(); DeviceAuth? auth;
 public MainWindow(){InitializeComponent();Loaded+=async(_,__)=>await InitializeAsync();}
 async Task InitializeAsync(){try{auth=SessionStore.Load()??await api.BootstrapAsync();SessionStore.Save(auth);DeviceIdText.Text=auth.DeviceId;DeviceKeyText.Text=auth.DeviceKey;StatusText.Text="Device ready";SubText.Text="Connected to Player One";ContinueButton.IsEnabled=true;}catch(Exception ex){StatusText.Text="Connection failed";SubText.Text=ex.Message;}}
 async void Continue_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;BootstrapView.Visibility=System.Windows.Visibility.Collapsed;HomeView.Visibility=System.Windows.Visibility.Visible;await SyncPlaylistsAsync();}
 async Task SyncPlaylistsAsync(){if(auth is null)return;try{using var d=await api.GetPlaylistsAsync(auth);if(d.RootElement.TryGetProperty("playlists",out var a)){var ids=a.EnumerateArray().Select(p=>p.TryGetProperty("id",out var id)?id.GetInt64():0).Where(x=>x>0).ToList();var selected=PlaylistSelectionStore.Load();if(ids.Count>0&&(!selected.HasValue||!ids.Contains(selected.Value)))PlaylistSelectionStore.Save(ids[0]);}}catch{}}
 void Settings_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;new SettingsWindow(auth).ShowDialog();_=SyncPlaylistsAsync();}
 void Library_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;new LibraryWindow(auth){Owner=this}.Show();}
 void Playlists_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;new PlaylistWindow(auth){Owner=this}.ShowDialog();_=SyncPlaylistsAsync();}
 void Info_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;new InfoWindow(auth){Owner=this}.ShowDialog();}
 void Live_Click(object s,System.Windows.RoutedEventArgs e){if(auth is null)return;new LiveWindow(auth).Show();}
 void Movies_Click(object s,System.Windows.RoutedEventArgs e)=>OpenCatalog("movie");
 void Series_Click(object s,System.Windows.RoutedEventArgs e)=>OpenCatalog("series");
 void OpenCatalog(string kind){if(auth is null)return;new CatalogWindow(auth,kind).Show();}
 void Exit_Click(object s,System.Windows.RoutedEventArgs e)=>ConfirmExit();
 void Window_KeyDown(object s,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape&&HomeView.Visibility==System.Windows.Visibility.Visible){ConfirmExit();e.Handled=true;}}
 void ConfirmExit(){if(!PlayerSettings.Load().ConfirmExit){System.Windows.Application.Current.Shutdown();return;}var result=System.Windows.MessageBox.Show("Do you want to exit the app?","Exit Player One",System.Windows.MessageBoxButton.YesNo,System.Windows.MessageBoxImage.Question);if(result==System.Windows.MessageBoxResult.Yes)System.Windows.Application.Current.Shutdown();}
}