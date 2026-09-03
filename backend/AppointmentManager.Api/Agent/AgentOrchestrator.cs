using System.Text.Json.Nodes;
using AppointmentManager.Api.Agent.Tools;
using AppointmentManager.Api.Data;
using AppointmentManager.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentManager.Api.Agent;

public class AgentOrchestrator(AppDbContext db, LiteLlmClient liteLlm, AppointmentTools tools)
{
    private const string SystemPrompt =
        """
        You are a helpful appointment booking assistant. You help users check provider
        availability, book, reschedule, and cancel appointments, and confirm bookings.

        Users only know providers, dates, and times in plain language — never internal
        database ids. Never ask the user for a provider id, slot id, or appointment id.
        When you need one, resolve it yourself: call list_providers to match a provider's
        name, or get_available_slots to match a requested day/time to a slot id. Only ask
        the user a clarifying question when the tool results themselves are ambiguous
        (e.g. multiple matching slots) — not to obtain an id you could look up.

        Always use the provided tools to look up real data and make real changes — never
        invent providers, slots, or appointment ids. After successfully booking or
        rescheduling an appointment, call send_confirmation and relay the confirmation to
        the user.
        """;

    private const int MaxToolCallRounds = 5;

    public async Task<(Guid sessionId, string reply)> HandleMessageAsync(Guid? sessionId, string userMessage, CancellationToken ct = default)
    {
        var session = sessionId.HasValue
            ? await db.Sessions.Include(s => s.Messages).FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            : null;

        if (session is null)
        {
            session = new ConversationSession();
            db.Sessions.Add(session);
        }

        session.Messages.Add(new ConversationMessage { SessionId = session.Id, Role = "user", Content = userMessage });
        await db.SaveChangesAsync(ct);

        var messages = BuildMessageHistory(session);
        var toolSchemas = ToolDefinitions.All();

        string? finalReply = null;

        for (var round = 0; round < MaxToolCallRounds; round++)
        {
            var completion = await liteLlm.CreateChatCompletionAsync(messages, toolSchemas, ct);
            var choice = completion["choices"]?[0]?["message"]?.AsObject()
                ?? throw new InvalidOperationException("LiteLLM response missing message.");

            var toolCalls = choice["tool_calls"]?.AsArray();

            if (toolCalls is null || toolCalls.Count == 0)
            {
                finalReply = choice["content"]?.GetValue<string>() ?? "";
                messages.Add(choice.DeepClone());
                break;
            }

            // Assistant turn requesting tool calls — keep in the running message list.
            messages.Add(choice.DeepClone());
            session.Messages.Add(new ConversationMessage
            {
                SessionId = session.Id,
                Role = "assistant",
                Content = choice["content"]?.GetValue<string>(),
                ToolCallsJson = toolCalls.ToJsonString()
            });

            foreach (var toolCallNode in toolCalls)
            {
                var toolCall = toolCallNode!.AsObject();
                var toolCallId = toolCall["id"]!.GetValue<string>();
                var function = toolCall["function"]!.AsObject();
                var toolName = function["name"]!.GetValue<string>();
                var argumentsJson = function["arguments"]?.GetValue<string>() ?? "{}";

                var result = await tools.ExecuteAsync(toolName, argumentsJson);

                var toolResultMessage = new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = toolCallId,
                    ["name"] = toolName,
                    ["content"] = result
                };
                messages.Add(toolResultMessage);

                session.Messages.Add(new ConversationMessage
                {
                    SessionId = session.Id,
                    Role = "tool",
                    Content = result,
                    ToolCallId = toolCallId,
                    ToolName = toolName
                });
            }

            await db.SaveChangesAsync(ct);
        }

        finalReply ??= "Sorry, I wasn't able to complete that — could you rephrase your request?";

        session.Messages.Add(new ConversationMessage { SessionId = session.Id, Role = "assistant", Content = finalReply });
        await db.SaveChangesAsync(ct);

        return (session.Id, finalReply);
    }

    private static JsonArray BuildMessageHistory(ConversationSession session)
    {
        var messages = new JsonArray
        {
            new JsonObject { ["role"] = "system", ["content"] = SystemPrompt }
        };

        foreach (var m in session.Messages.OrderBy(m => m.Id))
        {
            if (m.Role == "assistant" && m.ToolCallsJson is not null)
            {
                messages.Add(new JsonObject
                {
                    ["role"] = "assistant",
                    ["content"] = m.Content,
                    ["tool_calls"] = JsonNode.Parse(m.ToolCallsJson)
                });
            }
            else if (m.Role == "tool")
            {
                messages.Add(new JsonObject
                {
                    ["role"] = "tool",
                    ["tool_call_id"] = m.ToolCallId,
                    ["name"] = m.ToolName,
                    ["content"] = m.Content
                });
            }
            else
            {
                messages.Add(new JsonObject { ["role"] = m.Role, ["content"] = m.Content });
            }
        }

        return messages;
    }
}
