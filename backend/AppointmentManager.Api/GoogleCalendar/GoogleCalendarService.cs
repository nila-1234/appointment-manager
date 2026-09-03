using AppointmentManager.Api.Data;
using AppointmentManager.Api.Data.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Api.GoogleCalendar;

/// Wraps the Google Calendar API for the one shared calendar all providers'
/// appointments sync to. Every call is best-effort: if no account has been
/// connected yet (or the call fails), booking still succeeds locally — the
/// failure is logged, not thrown, so Calendar sync is a bonus, not a blocker.
public class GoogleCalendarService(AppDbContext db, GoogleCalendarOptions options, ILogger<GoogleCalendarService> logger)
{
    private GoogleAuthorizationCodeFlow BuildFlow() => new(new GoogleAuthorizationCodeFlow.Initializer
    {
        ClientSecrets = new ClientSecrets { ClientId = options.ClientId, ClientSecret = options.ClientSecret },
        Scopes = [CalendarService.Scope.Calendar]
    });

    public string BuildAuthorizationUrl()
    {
        var request = new GoogleAuthorizationCodeRequestUrl(new Uri("https://accounts.google.com/o/oauth2/v2/auth"))
        {
            ClientId = options.ClientId,
            Scope = CalendarService.Scope.Calendar,
            RedirectUri = options.RedirectUri,
            AccessType = "offline",
            Prompt = "consent"
        };
        return request.Build().ToString();
    }

    public async Task ExchangeCodeAndStoreAsync(string code, CancellationToken ct = default)
    {
        var flow = BuildFlow();
        var tokenResponse = await flow.ExchangeCodeForTokenAsync("user", code, options.RedirectUri, ct);

        if (string.IsNullOrEmpty(tokenResponse.RefreshToken))
        {
            throw new InvalidOperationException(
                "Google did not return a refresh token. Revoke prior access at " +
                "https://myaccount.google.com/permissions and try connecting again.");
        }

        // Single-row table: replace whatever was connected before.
        db.GoogleAuthTokens.RemoveRange(db.GoogleAuthTokens);
        db.GoogleAuthTokens.Add(new GoogleAuthToken
        {
            RefreshToken = tokenResponse.RefreshToken,
            AccessToken = tokenResponse.AccessToken,
            AccessTokenExpiresAt = tokenResponse.ExpiresInSeconds.HasValue
                ? tokenResponse.IssuedUtc.AddSeconds(tokenResponse.ExpiresInSeconds.Value)
                : null
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> IsConnectedAsync(CancellationToken ct = default) =>
        await db.GoogleAuthTokens.AnyAsync(ct);

    private async Task<CalendarService?> BuildClientAsync(CancellationToken ct)
    {
        var stored = await db.GoogleAuthTokens.AsNoTracking().FirstOrDefaultAsync(ct);
        if (stored is null) return null;

        var flow = BuildFlow();
        var credential = new UserCredential(flow, "user", new TokenResponse
        {
            RefreshToken = stored.RefreshToken,
            AccessToken = stored.AccessToken
        });

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Appointment Manager"
        });
    }

    public async Task<string?> CreateEventAsync(
        string providerName, string customerName, string customerEmail,
        DateTime start, DateTime end, CancellationToken ct = default)
    {
        var client = await BuildClientAsync(ct);
        if (client is null)
        {
            logger.LogInformation("Google Calendar not connected; skipping event creation.");
            return null;
        }

        var newEvent = new Event
        {
            Summary = $"Appointment: {customerName} with {providerName}",
            Description = $"Booked via Appointment Manager. Customer email: {customerEmail}",
            Start = new EventDateTime { DateTimeDateTimeOffset = start },
            End = new EventDateTime { DateTimeDateTimeOffset = end }
        };

        try
        {
            var created = await client.Events.Insert(newEvent, options.CalendarId).ExecuteAsync(ct);
            return created.Id;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create Google Calendar event.");
            return null;
        }
    }

    public async Task UpdateEventTimeAsync(string eventId, DateTime start, DateTime end, CancellationToken ct = default)
    {
        var client = await BuildClientAsync(ct);
        if (client is null) return;

        try
        {
            var existing = await client.Events.Get(options.CalendarId, eventId).ExecuteAsync(ct);
            existing.Start = new EventDateTime { DateTimeDateTimeOffset = start };
            existing.End = new EventDateTime { DateTimeDateTimeOffset = end };
            await client.Events.Update(existing, options.CalendarId, eventId).ExecuteAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update Google Calendar event {EventId}.", eventId);
        }
    }

    public async Task DeleteEventAsync(string eventId, CancellationToken ct = default)
    {
        var client = await BuildClientAsync(ct);
        if (client is null) return;

        try
        {
            await client.Events.Delete(options.CalendarId, eventId).ExecuteAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete Google Calendar event {EventId}.", eventId);
        }
    }
}
