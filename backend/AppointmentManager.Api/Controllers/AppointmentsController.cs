using AppointmentManager.Api.Data;
using AppointmentManager.Api.Data.Entities;
using AppointmentManager.Api.GoogleCalendar;
using AppointmentManager.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Api.Controllers;

/// Plain REST CRUD for appointments — useful for the frontend to render state
/// directly and for testing booking logic independent of the AI agent.
[ApiController]
[Route("api/appointments")]
public class AppointmentsController(AppDbContext db, GoogleCalendarService calendar) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AppointmentDto>>> GetAppointments()
    {
        var appointments = await db.Appointments
            .Include(a => a.Provider)
            .Include(a => a.Slot)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AppointmentDto(
                a.Id, a.ProviderId, a.Provider!.Name, a.SlotId,
                a.Slot!.StartTime, a.Slot.EndTime,
                a.CustomerName, a.CustomerEmail, a.Status.ToString()))
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Book([FromBody] BookAppointmentRequest request)
    {
        var slot = await db.Slots.Include(s => s.Provider).FirstOrDefaultAsync(s => s.Id == request.SlotId);
        if (slot is null) return NotFound("Slot not found.");
        if (slot.IsBooked) return Conflict("Slot already booked.");

        slot.IsBooked = true;
        var appointment = new Appointment
        {
            ProviderId = slot.ProviderId,
            SlotId = slot.Id,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            Status = AppointmentStatus.Booked
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        appointment.GoogleEventId = await calendar.CreateEventAsync(
            slot.Provider?.Name ?? "provider", appointment.CustomerName, appointment.CustomerEmail,
            slot.StartTime, slot.EndTime);
        if (appointment.GoogleEventId is not null)
            await db.SaveChangesAsync();

        return Ok(new AppointmentDto(
            appointment.Id, slot.ProviderId, slot.Provider?.Name ?? "", slot.Id,
            slot.StartTime, slot.EndTime, appointment.CustomerName, appointment.CustomerEmail,
            appointment.Status.ToString()));
    }

    [HttpPost("{id:int}/reschedule")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(int id, [FromBody] RescheduleAppointmentRequest request)
    {
        var appointment = await db.Appointments.Include(a => a.Slot).FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null) return NotFound("Appointment not found.");

        var newSlot = await db.Slots.FirstOrDefaultAsync(s => s.Id == request.NewSlotId);
        if (newSlot is null) return NotFound("New slot not found.");
        if (newSlot.IsBooked) return Conflict("New slot already booked.");

        if (appointment.Slot is not null) appointment.Slot.IsBooked = false;
        newSlot.IsBooked = true;
        appointment.SlotId = newSlot.Id;
        appointment.ProviderId = newSlot.ProviderId;
        appointment.Status = AppointmentStatus.Rescheduled;
        await db.SaveChangesAsync();

        if (appointment.GoogleEventId is not null)
            await calendar.UpdateEventTimeAsync(appointment.GoogleEventId, newSlot.StartTime, newSlot.EndTime);

        var provider = await db.Providers.FindAsync(newSlot.ProviderId);

        return Ok(new AppointmentDto(
            appointment.Id, newSlot.ProviderId, provider?.Name ?? "", newSlot.Id,
            newSlot.StartTime, newSlot.EndTime, appointment.CustomerName, appointment.CustomerEmail,
            appointment.Status.ToString()));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var appointment = await db.Appointments.Include(a => a.Slot).FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null) return NotFound("Appointment not found.");

        if (appointment.Slot is not null) appointment.Slot.IsBooked = false;
        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync();

        if (appointment.GoogleEventId is not null)
            await calendar.DeleteEventAsync(appointment.GoogleEventId);

        return NoContent();
    }
}
