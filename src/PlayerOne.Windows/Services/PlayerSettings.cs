using System.Text.Json;
namespace PlayerOne.Windows.Services;
public sealed record PlayerSettingsData(bool RememberLastSection=false,string StartSection="home",bool ConfirmExit=true,bool AutoReconnect=true,string DefaultAspect="fill",string FocusStrength="normal",bool BackgroundPlayback=false,bool SubtitlesEnabled=true,string PreferredAudioLanguage="auto",string PreferredSubtitleLanguage="auto",string CatalogStart="recent",string AppLanguage="ar");
public static class PlayerSettings {
 static string Dir=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Player One");
 static string FilePath=>Path.Combine(Dir,"settings.json");
 public static PlayerSettingsData Load(){try{return File.Exists(FilePath)?JsonSerializer.Deserialize<PlayerSettingsData>(File.ReadAllText(FilePath))??new():new();}catch{return new();}}
 public static void Save(PlayerSettingsData x){Directory.CreateDirectory(Dir);File.WriteAllText(FilePath,JsonSerializer.Serialize(x));}
}
