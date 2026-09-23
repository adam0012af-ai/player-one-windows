using PlayerOne.Windows.Models;
using PlayerOne.Windows.Services;

namespace PlayerOne.Windows;
public partial class MainWindow : System.Windows.Window {
 readonly PlayerOneApi api=new(); DeviceAuth? auth;
 public MainWindow(){InitializeComponent();Loaded+=async(_,__)=>await InitializeAsync();}
 async Task InitializeAsync(){
  try{
   auth=SessionStore.Load() ?? await api.BootstrapAsync();
   SessionStore.Save(auth);
   using var status=await api.GetStatusAsync(auth);
   DeviceIdText.Text=auth.DeviceId; DeviceKeyText.Text=auth.DeviceKey;
   StatusText.Text="Device ready";
   SubText.Text="Connected to the same Player One device and activation system.";
   ContinueButton.IsEnabled=true;
  }catch(Exception ex){StatusText.Text="Connection failed";SubText.Text=ex.Message;}
 }
 async void Continue_Click(object sender,System.Windows.RoutedEventArgs e){
  if(auth is null)return;
  try{
   using var playlists=await api.GetPlaylistsAsync(auth);
   var count=playlists.RootElement.TryGetProperty("playlists",out var p)&&p.ValueKind==System.Text.Json.JsonValueKind.Array?p.GetArrayLength():0;
   StatusText.Text="Player One ready";SubText.Text=$"{count} playlist(s) linked to this Windows device.";
  }catch(Exception ex){SubText.Text=ex.Message;}
 }
}
