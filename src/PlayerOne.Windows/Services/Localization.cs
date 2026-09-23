namespace PlayerOne.Windows.Services;
public static class Localization{
 public static bool Arabic=>PlayerSettings.Load().AppLanguage=="ar";
 public static System.Windows.FlowDirection Flow=>Arabic?System.Windows.FlowDirection.RightToLeft:System.Windows.FlowDirection.LeftToRight;
 public static string T(string ar,string en)=>Arabic?ar:en;
}