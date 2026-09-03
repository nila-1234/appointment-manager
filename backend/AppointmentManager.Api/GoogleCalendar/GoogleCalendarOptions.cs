namespace AppointmentManager.Api.GoogleCalendar;

public class GoogleCalendarOptions
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    /// Must exactly match an authorized redirect URI on the OAuth client in Google Cloud Console.
    public string RedirectUri { get; set; } = "http://localhost:5080/api/google/auth/callback";

    /// "primary" uses the connected account's main calendar.
    public string CalendarId { get; set; } = "primary";
}
