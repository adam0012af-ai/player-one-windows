namespace PlayerOne.Windows.Services;
public static class PlaylistSelectionStore {
 static string Dir=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Player One");
 static string FilePath=>Path.Combine(Dir,"selected-playlist.txt");
 public static long? Load(){try{return File.Exists(FilePath)&&long.TryParse(File.ReadAllText(FilePath),out var id)?id:null;}catch{return null;}}
 public static void Save(long id){Directory.CreateDirectory(Dir);File.WriteAllText(FilePath,id.ToString());}
 public static void Clear(){try{if(File.Exists(FilePath))File.Delete(FilePath);}catch{}}
}
