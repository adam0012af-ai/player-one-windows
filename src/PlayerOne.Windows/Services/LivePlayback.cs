namespace PlayerOne.Windows.Services;
public static class LivePlayback{
 public static IEnumerable<string> Candidates(string url){yield return url;var alt=AdvancedSettings.AlternateLiveUrl(url);if(!string.IsNullOrWhiteSpace(alt))yield return alt;}
}