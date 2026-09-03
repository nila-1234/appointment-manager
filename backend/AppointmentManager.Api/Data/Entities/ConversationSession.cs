namespace AppointmentManager.Api.Data.Entities;

public class ConversationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ConversationMessage> Messages { get; set; } = [];
}
