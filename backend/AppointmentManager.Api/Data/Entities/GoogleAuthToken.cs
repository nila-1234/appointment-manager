namespace AppointmentManager.Api.Data.Entities;

/// Single-row table holding the refresh token for the one Google account
/// that authorized calendar access. Re-created on each new /login consent.
public class GoogleAuthToken
{
    public int Id { get; set; }
    public required string RefreshToken { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? AccessTokenExpiresAt { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}
