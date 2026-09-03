namespace AppointmentManager.Api.Data.Entities;

public class Provider
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Specialty { get; set; }

    public List<AvailabilitySlot> Slots { get; set; } = [];
}
