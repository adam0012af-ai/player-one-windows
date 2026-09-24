using PlayerOne.Windows.Models;using PlayerOne.Windows.Services;
namespace PlayerOne.Windows;
public partial class InfoWindow:System.Windows.Window{
 readonly DeviceAuth auth;readonly PlayerOneApi api=new();
 public InfoWindow(DeviceAuth a){InitializeComponent();auth=a;Loaded+=LoadAsync;}
 async void LoadAsync(object? sender,System.Windows.RoutedEventArgs e){try{using var d=await api.GetStatusAsync(auth);var x=d.RootElement.TryGetProperty("device",out var z)?z:d.RootElement;var state=x.TryGetProperty("status",out var s)?s.ToString():"unknown";var expiry=x.TryGetProperty("expiresAt",out var ex)?ex.ToString():(x.TryGetProperty("trialExpiresAt",out var tr)?tr.ToString():"—");var plan=x.TryGetProperty("plan",out var pl)?pl.ToString():"—";Status.Text=$"Status: {state}\nActivation duration: {Plan(plan)}";Expiry.Text="Activation expiry: "+Short(expiry);Device.Text=$"Device ID: {auth.DeviceId}\nDevice Key: {auth.DeviceKey}\nDevice type: {DeviceIdentity.Label()}";}catch(Exception ex){Status.Text=ex.Message;}}
 static string Plan(string? value)=>value?.ToUpperInvariant() switch{"YEAR"=>"12 months","LIFETIME"=>"Lifetime",_=>string.IsNullOrWhiteSpace(value)?"—":value};
 static string Short(string? value)=>string.IsNullOrWhiteSpace(value)?"—":value.Split('T')[0];
}