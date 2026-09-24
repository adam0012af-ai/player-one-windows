namespace PlayerOne.Windows.Models;
public sealed record DeviceAuth(string Api,string DeviceId,string DeviceKey,string? Status,string? TrialExpiresAt,string? ExpiresAt);
public sealed record DevicePlaylist(long Id,string Name,string Type,string? AppActivationPlan,string? AppActivationExpiresAt);
public sealed record PlaylistConfig(long Id,string Name,string Type,string? Url,string? Host,string? Username,string? Password);

public sealed record DeviceSnapshot(string Status,string? PlanCode,string? TrialExpiresAt,string? ExpiresAt,string? ActivatedAt,int PlaylistCount);
