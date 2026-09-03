using AppointmentManager.Api.Data;
using AppointmentManager.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Api.Controllers;

[ApiController]
[Route("api/slots")]
public class SlotsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SlotDto>>> GetSlots([FromQuery] int? providerId, [FromQuery] bool onlyAvailable = true)
    {
        var query = db.Slots.AsQueryable();

        if (providerId.HasValue)
            query = query.Where(s => s.ProviderId == providerId.Value);

        if (onlyAvailable)
            query = query.Where(s => !s.IsBooked);

        var slots = await query
            .OrderBy(s => s.StartTime)
            .Select(s => new SlotDto(s.Id, s.ProviderId, s.StartTime, s.EndTime, s.IsBooked))
            .ToListAsync();

        return Ok(slots);
    }
}
