using PlayerOne.Windows.Models;using PlayerOne.Windows.Services;namespace PlayerOne.Windows;
public partial class SeriesWindow:System.Windows.Window{readonly DeviceAuth auth;readonly XtreamItem series;readonly string host,user,pass;List<EpisodeItem> all=[];
public SeriesWindow(DeviceAuth a,XtreamItem s,string h,string u,string p){InitializeComponent();auth=a;series=s;host=h;user=u;pass=p;Title=s.Name;Loaded+=async(_,__)=>{all=await new SeriesService().LoadXtreamAsync(host,user,pass,series.Id);Seasons.ItemsSource=all.Select(x=>x.Season).Distinct().Order().ToList();if(Seasons.Items.Count>0)Seasons.SelectedIndex=0;};KeyDown+=OnKey;}
void Season_Changed(object s,System.Windows.Controls.SelectionChangedEventArgs e){if(Seasons.SelectedItem is int n)Episodes.ItemsSource=all.Where(x=>x.Season==n).OrderBy(x=>x.Episode);}
void Episode_DoubleClick(object s,System.Windows.Input.MouseButtonEventArgs e){if(Episodes.SelectedItem is EpisodeItem ep)Play(ep);}
void OnKey(object s,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Enter&&Episodes.SelectedItem is EpisodeItem ep)Play(ep);}
void Play(EpisodeItem ep){var index=all.FindIndex(x=>x.Id==ep.Id);var queue=all.Skip(index).Select(x=>new XtreamItem(x.Id,x.Title,series.Name,series.Image,x.StreamUrl,"series",0)).ToList();new PlayerWindow(auth,queue[0],queue).Show();}
}