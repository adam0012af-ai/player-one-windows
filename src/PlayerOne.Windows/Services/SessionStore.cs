using System.Text.Json;
using PlayerOne.Windows.Models;

namespace PlayerOne.Windows.Services;
public static class SessionStore {
  static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Player One");
  static string FilePath => Path.Combine(Dir,"device.json");
  public static void Save(DeviceAuth auth){Directory.CreateDirectory(Dir);File.WriteAllText(FilePath,JsonSerializer.Serialize(auth));}
  public static DeviceAuth? Load(){try{return File.Exists(FilePath)?JsonSerializer.Deserialize<DeviceAuth>(File.ReadAllText(FilePath)):null;}catch{return null;}}
}
