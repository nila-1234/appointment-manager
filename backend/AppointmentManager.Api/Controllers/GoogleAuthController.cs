using AppointmentManager.Api.GoogleCalendar;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentManager.Api.Controllers;

/// One-time admin flow to connect the shared Google Calendar. Visit /api/google/auth/login
/// in a browser, grant consent, and the refresh token is stored for all future syncing.
[ApiController]
[Route("api/google/auth")]
public class GoogleAuthController(GoogleCalendarService calendar) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult Login()
    {
        var url = calendar.BuildAuthorizationUrl();
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? error, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
            return BadRequest($"Google returned an error: {error}");
        if (string.IsNullOrEmpty(code))
            return BadRequest("Missing authorization code.");

        await calendar.ExchangeCodeAndStoreAsync(code, ct);
        return Content("Google Calendar connected. You can close this tab.", "text/plain");
    }

    [HttpGet("status")]
    public async Task<ActionResult<object>> Status(CancellationToken ct)
    {
        var connected = await calendar.IsConnectedAsync(ct);
        return Ok(new { connected });
    }
}
