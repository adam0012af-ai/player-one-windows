using PlayerOne.Windows.Models;using PlayerOne.Windows.Services;
namespace PlayerOne.Windows;
public partial class MovieDetailsWindow:System.Windows.Window{
 readonly DeviceAuth auth;readonly XtreamItem item;readonly string h,u,p;
 public MovieDetailsWindow(DeviceAuth a,XtreamItem i,string host,string user,string pass){InitializeComponent();auth=a;item=i;h=host;u=user;p=pass;MovieTitle.Text=i.Name;Fav.Content=PlayerStateStore.IsFavorite(auth,PlaylistSelectionStore.Load(),item.Kind,item.Id)?"Favorited":"Favorite";Loaded+=async(_,__)=>{try{var x=await new XtreamClient().MovieInfoAsync(h,u,p,i.Id);Meta.Text=string.Join(" • ",new[]{x.Year,x.Duration,x.Rating}.Where(v=>!string.IsNullOrWhiteSpace(v)));Plot.Text=x.Plot??"";Credits.Text=$"{x.Genre}\n{x.Director}\n{x.Cast}";if(Uri.TryCreate(i.Image,UriKind.Absolute,out var uri))Poster.Source=new System.Windows.Media.Imaging.BitmapImage(uri);}catch{}};KeyDown+=OnKey;}
 void Play_Click(object s,System.Windows.RoutedEventArgs e){if(item.StreamUrl is not null)new PlayerWindow(auth,item).Show();}
 void Fav_Click(object s,System.Windows.RoutedEventArgs e){var added=PlayerStateStore.ToggleFavorite(auth,PlaylistSelectionStore.Load(),new(item.Kind,item.Id,item.Name,item.Group,item.Image,item.StreamUrl,DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));Fav.Content=added?"Favorited":"Favorite";}
 void Back_Click(object s,System.Windows.RoutedEventArgs e)=>Close();
 void OnKey(object s,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape){Close();e.Handled=true;}}
}