namespace AppointmentManager.Api.Models;

public record ProviderDto(int Id, string Name, string? Specialty);

public record SlotDto(int Id, int ProviderId, DateTime StartTime, DateTime EndTime, bool IsBooked);

public record AppointmentDto(
    int Id,
    int ProviderId,
    string ProviderName,
    int SlotId,
    DateTime StartTime,
    DateTime EndTime,
    string CustomerName,
    string CustomerEmail,
    string Status);

public record BookAppointmentRequest(int SlotId, string CustomerName, string CustomerEmail);

public record RescheduleAppointmentRequest(int NewSlotId);

public record ChatRequest(Guid? SessionId, string Message);

public record ChatResponse(Guid SessionId, string Reply);
