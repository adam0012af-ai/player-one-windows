using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text;

namespace PlayerOne.Windows.Services;
public static class DeviceIdentity {
  public static string HardwareId() {
    string machine = "";
    try { machine = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography","MachineGuid","")?.ToString() ?? ""; } catch {}
    var raw = $"{Environment.MachineName}|{machine}|{Environment.OSVersion.VersionString}|PLAYER_ONE_WINDOWS";
    byte[] hash;
    using (var sha = SHA256.Create())
    {
      hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
    }
    return "WIN-" + BitConverter.ToString(hash, 0, 12).Replace("-", string.Empty);
  }
  public static string Brand() => Environment.MachineName;
  public static string Label() => $"windows_pc|{Environment.MachineName}|{System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}";
}
