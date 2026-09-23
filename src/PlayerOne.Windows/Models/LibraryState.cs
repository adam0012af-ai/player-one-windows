namespace PlayerOne.Windows.Models;
public sealed record RecentEntry(string Type,string Title,string Subtitle,string? Image,string? StreamUrl,long PositionMs,long DurationMs,long UpdatedAt);
public sealed record FavoriteEntry(string Type,string Id,string Title,string Subtitle,string? Image,string? StreamUrl,long UpdatedAt);
