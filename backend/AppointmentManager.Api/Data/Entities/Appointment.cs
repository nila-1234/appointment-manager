namespace AppointmentManager.Api.Data.Entities;

public class Appointment
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public Provider? Provider { get; set; }
    public int SlotId { get; set; }
    public AvailabilitySlot? Slot { get; set; }

    public required string CustomerName { get; set; }
    public required string CustomerEmail { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// Id of the corresponding Google Calendar event, if calendar sync is connected.
    public string? GoogleEventId { get; set; }
}
