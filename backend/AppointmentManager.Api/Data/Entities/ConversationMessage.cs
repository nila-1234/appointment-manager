namespace AppointmentManager.Api.Data.Entities;

public class ConversationMessage
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public ConversationSession? Session { get; set; }

    /// user | assistant | tool | system
    public required string Role { get; set; }
    public string? Content { get; set; }

    /// Serialized JSON of the assistant's tool_calls array, when present.
    public string? ToolCallsJson { get; set; }

    /// Set on tool-role messages: the id of the tool_call this responds to.
    public string? ToolCallId { get; set; }

    /// Set on tool-role messages: the name of the tool that was invoked.
    public string? ToolName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
