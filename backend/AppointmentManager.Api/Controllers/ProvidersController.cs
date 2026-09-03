using AppointmentManager.Api.Data;
using AppointmentManager.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Api.Controllers;

[ApiController]
[Route("api/providers")]
public class ProvidersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProviderDto>>> GetProviders()
    {
        var providers = await db.Providers
            .Select(p => new ProviderDto(p.Id, p.Name, p.Specialty))
            .ToListAsync();
        return Ok(providers);
    }
}
