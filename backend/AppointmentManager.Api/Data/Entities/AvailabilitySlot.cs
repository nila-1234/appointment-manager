namespace AppointmentManager.Api.Data.Entities;

public class AvailabilitySlot
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public Provider? Provider { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsBooked { get; set; }
}
