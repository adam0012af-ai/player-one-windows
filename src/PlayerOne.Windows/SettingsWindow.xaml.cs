using PlayerOne.Windows.Models;using PlayerOne.Windows.Services;
namespace PlayerOne.Windows;
public partial class SettingsWindow:System.Windows.Window{
 readonly DeviceAuth? auth;
 public SettingsWindow():this(null){}
 public SettingsWindow(DeviceAuth? deviceAuth){InitializeComponent();auth=deviceAuth;var x=PlayerSettings.Load();Reconnect.IsChecked=x.AutoReconnect;Subs.IsChecked=x.SubtitlesEnabled;var adv=AdvancedSettings.Load();AutoNext.IsChecked=adv.AutoNext;KeepScreen.IsChecked=adv.KeepScreenOn;Select(StreamFormat,adv.StreamingFormat);Select(BufferProfile,adv.BufferProfile);Select(Aspect,x.DefaultAspect);Select(Language,x.AppLanguage);Select(Audio,x.PreferredAudioLanguage);Select(Subtitle,x.PreferredSubtitleLanguage);}
 void Back_Click(object s,System.Windows.RoutedEventArgs e)=>Close();
 static void Select(System.Windows.Controls.ComboBox b,string v){foreach(System.Windows.Controls.ComboBoxItem i in b.Items)if(i.Content?.ToString()==v){b.SelectedItem=i;break;}}
 static string Val(System.Windows.Controls.ComboBox b,string d)=>(b.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString()??d;
 void Save_Click(object s,System.Windows.RoutedEventArgs e){var o=PlayerSettings.Load();PlayerSettings.Save(o with{AutoReconnect=Reconnect.IsChecked==true,SubtitlesEnabled=Subs.IsChecked==true,DefaultAspect=Val(Aspect,"fill"),AppLanguage=Val(Language,"ar"),PreferredAudioLanguage=Val(Audio,"auto"),PreferredSubtitleLanguage=Val(Subtitle,"auto")});var adv=AdvancedSettings.Load();AdvancedSettings.Save(adv with{StreamingFormat=Val(StreamFormat,"auto"),BufferProfile=Val(BufferProfile,"balanced"),KeepScreenOn=KeepScreen.IsChecked==true,AutoNext=AutoNext.IsChecked==true});Close();}
}