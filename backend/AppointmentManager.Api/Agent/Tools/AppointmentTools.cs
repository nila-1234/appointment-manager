using System.Text.Json;
using AppointmentManager.Api.Data;
using AppointmentManager.Api.Data.Entities;
using AppointmentManager.Api.GoogleCalendar;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Api.Agent.Tools;

/// Executes tool calls the model requests, against the real database.
/// Every method returns a JSON string that gets fed back to the model as the tool result.
public class AppointmentTools(AppDbContext db, GoogleCalendarService calendar)
{
    public async Task<string> ExecuteAsync(string toolName, string argumentsJson)
    {
        using var doc = string.IsNullOrWhiteSpace(argumentsJson)
            ? JsonDocument.Parse("{}")
            : JsonDocument.Parse(argumentsJson);
        var args = doc.RootElement;

        return toolName switch
        {
            "list_providers" => await ListProvidersAsync(),
            "get_available_slots" => await GetAvailableSlotsAsync(args),
            "book_appointment" => await BookAppointmentAsync(args),
            "reschedule_appointment" => await RescheduleAppointmentAsync(args),
            "cancel_appointment" => await CancelAppointmentAsync(args),
            "send_confirmation" => await SendConfirmationAsync(args),
            _ => JsonSerializer.Serialize(new { error = $"Unknown tool '{toolName}'" })
        };
    }

    private async Task<string> ListProvidersAsync()
    {
        var providers = await db.Providers
            .Select(p => new { p.Id, p.Name, p.Specialty })
            .ToListAsync();

        return JsonSerializer.Serialize(providers);
    }

    private async Task<string> GetAvailableSlotsAsync(JsonElement args)
    {
        var providerId = args.GetProperty("provider_id").GetInt32();

        var query = db.Slots.Where(s => s.ProviderId == providerId && !s.IsBooked);

        if (args.TryGetProperty("from_date", out var fromProp) &&
            DateTime.TryParse(fromProp.GetString(), out var from))
        {
            query = query.Where(s => s.StartTime >= from);
        }

        if (args.TryGetProperty("to_date", out var toProp) &&
            DateTime.TryParse(toProp.GetString(), out var to))
        {
            // A date-only value (e.g. "2026-09-04", meaning "that whole day") parses to
            // midnight, which would otherwise exclude every slot that day — treat it as
            // through end-of-day instead of literally midnight.
            var toExclusive = to.TimeOfDay == TimeSpan.Zero ? to.AddDays(1) : to;
            query = query.Where(s => s.StartTime < toExclusive);
        }

        var slots = await query
            .OrderBy(s => s.StartTime)
            .Select(s => new { s.Id, s.StartTime, s.EndTime })
            .Take(20)
            .ToListAsync();

        return JsonSerializer.Serialize(slots);
    }

    private async Task<string> BookAppointmentAsync(JsonElement args)
    {
        var slotId = args.GetProperty("slot_id").GetInt32();
        var customerName = args.GetProperty("customer_name").GetString() ?? "";
        var customerEmail = args.GetProperty("customer_email").GetString() ?? "";

        var slot = await db.Slots.Include(s => s.Provider).FirstOrDefaultAsync(s => s.Id == slotId);
        if (slot is null)
            return JsonSerializer.Serialize(new { error = "Slot not found." });
        if (slot.IsBooked)
            return JsonSerializer.Serialize(new { error = "That slot is already booked. Please choose another." });

        slot.IsBooked = true;

        var appointment = new Appointment
        {
            ProviderId = slot.ProviderId,
            SlotId = slot.Id,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Status = AppointmentStatus.Booked
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        appointment.GoogleEventId = await calendar.CreateEventAsync(
            slot.Provider?.Name ?? "provider", customerName, customerEmail, slot.StartTime, slot.EndTime);
        if (appointment.GoogleEventId is not null)
            await db.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            appointment_id = appointment.Id,
            provider_id = slot.ProviderId,
            start_time = slot.StartTime,
            end_time = slot.EndTime,
            status = appointment.Status.ToString(),
            synced_to_google_calendar = appointment.GoogleEventId is not null
        });
    }

    private async Task<string> RescheduleAppointmentAsync(JsonElement args)
    {
        var appointmentId = args.GetProperty("appointment_id").GetInt32();
        var newSlotId = args.GetProperty("new_slot_id").GetInt32();

        var appointment = await db.Appointments.Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null)
            return JsonSerializer.Serialize(new { error = "Appointment not found." });

        var newSlot = await db.Slots.FirstOrDefaultAsync(s => s.Id == newSlotId);
        if (newSlot is null)
            return JsonSerializer.Serialize(new { error = "New slot not found." });
        if (newSlot.IsBooked)
            return JsonSerializer.Serialize(new { error = "That slot is already booked. Please choose another." });

        if (appointment.Slot is not null)
            appointment.Slot.IsBooked = false;

        newSlot.IsBooked = true;
        appointment.SlotId = newSlot.Id;
        appointment.ProviderId = newSlot.ProviderId;
        appointment.Status = AppointmentStatus.Rescheduled;

        await db.SaveChangesAsync();

        if (appointment.GoogleEventId is not null)
            await calendar.UpdateEventTimeAsync(appointment.GoogleEventId, newSlot.StartTime, newSlot.EndTime);

        return JsonSerializer.Serialize(new
        {
            appointment_id = appointment.Id,
            provider_id = newSlot.ProviderId,
            start_time = newSlot.StartTime,
            end_time = newSlot.EndTime,
            status = appointment.Status.ToString()
        });
    }

    private async Task<string> CancelAppointmentAsync(JsonElement args)
    {
        var appointmentId = args.GetProperty("appointment_id").GetInt32();

        var appointment = await db.Appointments.Include(a => a.Slot)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null)
            return JsonSerializer.Serialize(new { error = "Appointment not found." });

        if (appointment.Slot is not null)
            appointment.Slot.IsBooked = false;

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync();

        if (appointment.GoogleEventId is not null)
            await calendar.DeleteEventAsync(appointment.GoogleEventId);

        return JsonSerializer.Serialize(new { appointment_id = appointment.Id, status = appointment.Status.ToString() });
    }

    private async Task<string> SendConfirmationAsync(JsonElement args)
    {
        var appointmentId = args.GetProperty("appointment_id").GetInt32();

        var appointment = await db.Appointments
            .Include(a => a.Slot)
            .Include(a => a.Provider)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment is null)
            return JsonSerializer.Serialize(new { error = "Appointment not found." });

        // Stubbed: in a real system this would send an email/SMS. Here we just
        // return the confirmation text so the assistant can relay it to the user.
        var confirmationText =
            $"Confirmed: {appointment.CustomerName} with {appointment.Provider?.Name} " +
            $"on {appointment.Slot?.StartTime:f}. A confirmation has been sent to {appointment.CustomerEmail}.";

        return JsonSerializer.Serialize(new { appointment_id = appointment.Id, confirmation_text = confirmationText });
    }
}
