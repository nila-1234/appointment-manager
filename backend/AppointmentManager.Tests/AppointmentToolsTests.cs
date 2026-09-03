using System.Text.Json;
using AppointmentManager.Api.Agent.Tools;
using AppointmentManager.Api.Data;
using AppointmentManager.Api.Data.Entities;
using AppointmentManager.Api.GoogleCalendar;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AppointmentManager.Tests;

public class AppointmentToolsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly AppointmentTools _tools;

    public AppointmentToolsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        // No Google credentials configured, so calendar sync is a no-op in tests
        // (AppointmentTools treats it as "not connected" and skips it).
        var calendar = new GoogleCalendarService(_db, new GoogleCalendarOptions(), NullLogger<GoogleCalendarService>.Instance);
        _tools = new AppointmentTools(_db, calendar);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private int SeedProviderWithSlot(out int slotId)
    {
        var provider = new Provider { Name = "Dr. Test", Specialty = "General" };
        _db.Providers.Add(provider);
        _db.SaveChanges();

        var slot = new AvailabilitySlot
        {
            ProviderId = provider.Id,
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30)
        };
        _db.Slots.Add(slot);
        _db.SaveChanges();

        slotId = slot.Id;
        return provider.Id;
    }

    [Fact]
    public async Task BookAppointment_MarksSlotBooked_AndCreatesAppointment()
    {
        SeedProviderWithSlot(out var slotId);

        var result = await _tools.ExecuteAsync("book_appointment",
            JsonSerializer.Serialize(new { slot_id = slotId, customer_name = "Jane Doe", customer_email = "jane@example.com" }));

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("appointment_id", out _));

        var slot = await _db.Slots.FindAsync(slotId);
        Assert.True(slot!.IsBooked);

        var appointment = await _db.Appointments.SingleAsync();
        Assert.Equal("Jane Doe", appointment.CustomerName);
        Assert.Equal(AppointmentStatus.Booked, appointment.Status);
    }

    [Fact]
    public async Task BookAppointment_AlreadyBookedSlot_ReturnsError()
    {
        SeedProviderWithSlot(out var slotId);
        var slot = await _db.Slots.FindAsync(slotId);
        slot!.IsBooked = true;
        await _db.SaveChangesAsync();

        var result = await _tools.ExecuteAsync("book_appointment",
            JsonSerializer.Serialize(new { slot_id = slotId, customer_name = "Jane Doe", customer_email = "jane@example.com" }));

        using var doc = JsonDocument.Parse(result);
        Assert.True(doc.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task RescheduleAppointment_FreesOldSlot_AndBooksNewSlot()
    {
        var providerId = SeedProviderWithSlot(out var oldSlotId);
        var newSlot = new AvailabilitySlot
        {
            ProviderId = providerId,
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddMinutes(30)
        };
        _db.Slots.Add(newSlot);
        await _db.SaveChangesAsync();

        var bookResult = await _tools.ExecuteAsync("book_appointment",
            JsonSerializer.Serialize(new { slot_id = oldSlotId, customer_name = "Jane Doe", customer_email = "jane@example.com" }));
        var appointmentId = JsonDocument.Parse(bookResult).RootElement.GetProperty("appointment_id").GetInt32();

        await _tools.ExecuteAsync("reschedule_appointment",
            JsonSerializer.Serialize(new { appointment_id = appointmentId, new_slot_id = newSlot.Id }));

        var oldSlot = await _db.Slots.FindAsync(oldSlotId);
        var refreshedNewSlot = await _db.Slots.FindAsync(newSlot.Id);
        var appointment = await _db.Appointments.FindAsync(appointmentId);

        Assert.False(oldSlot!.IsBooked);
        Assert.True(refreshedNewSlot!.IsBooked);
        Assert.Equal(AppointmentStatus.Rescheduled, appointment!.Status);
    }

    [Fact]
    public async Task CancelAppointment_FreesSlot_AndMarksCancelled()
    {
        SeedProviderWithSlot(out var slotId);

        var bookResult = await _tools.ExecuteAsync("book_appointment",
            JsonSerializer.Serialize(new { slot_id = slotId, customer_name = "Jane Doe", customer_email = "jane@example.com" }));
        var appointmentId = JsonDocument.Parse(bookResult).RootElement.GetProperty("appointment_id").GetInt32();

        await _tools.ExecuteAsync("cancel_appointment", JsonSerializer.Serialize(new { appointment_id = appointmentId }));

        var slot = await _db.Slots.FindAsync(slotId);
        var appointment = await _db.Appointments.FindAsync(appointmentId);

        Assert.False(slot!.IsBooked);
        Assert.Equal(AppointmentStatus.Cancelled, appointment!.Status);
    }
}
