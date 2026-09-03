using System.Text.Json;
using System.Text.Json.Nodes;

namespace AppointmentManager.Api.Agent.Tools;

/// OpenAI-compatible tool/function schemas passed to LiteLLM on every chat turn.
public static class ToolDefinitions
{
    public static JsonArray All()
    {
        return new JsonArray(
            Tool(
                "list_providers",
                "List all providers (doctors/specialists) available for booking, with their specialty.",
                new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() }
            ),
            Tool(
                "get_available_slots",
                "Get open appointment slots for a specific provider, optionally within a date range.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["provider_id"] = Prop("integer", "The provider's id, from list_providers."),
                        ["from_date"] = Prop("string", "ISO 8601 date to start searching from (optional)."),
                        ["to_date"] = Prop("string", "ISO 8601 date to search until (optional).")
                    },
                    ["required"] = new JsonArray("provider_id")
                }
            ),
            Tool(
                "book_appointment",
                "Book an appointment for a customer in a specific open slot.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["slot_id"] = Prop("integer", "The id of the slot to book, from get_available_slots."),
                        ["customer_name"] = Prop("string", "Full name of the customer."),
                        ["customer_email"] = Prop("string", "Email address of the customer.")
                    },
                    ["required"] = new JsonArray("slot_id", "customer_name", "customer_email")
                }
            ),
            Tool(
                "reschedule_appointment",
                "Move an existing appointment to a different open slot.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["appointment_id"] = Prop("integer", "The id of the existing appointment."),
                        ["new_slot_id"] = Prop("integer", "The id of the new open slot to move it to.")
                    },
                    ["required"] = new JsonArray("appointment_id", "new_slot_id")
                }
            ),
            Tool(
                "cancel_appointment",
                "Cancel an existing appointment and free up its slot.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["appointment_id"] = Prop("integer", "The id of the appointment to cancel.")
                    },
                    ["required"] = new JsonArray("appointment_id")
                }
            ),
            Tool(
                "send_confirmation",
                "Send (or generate) a confirmation message for a booked or rescheduled appointment.",
                new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["appointment_id"] = Prop("integer", "The id of the appointment to confirm.")
                    },
                    ["required"] = new JsonArray("appointment_id")
                }
            )
        );
    }

    private static JsonObject Prop(string type, string description) =>
        new() { ["type"] = type, ["description"] = description };

    private static JsonObject Tool(string name, string description, JsonObject parameters) =>
        new()
        {
            ["type"] = "function",
            ["function"] = new JsonObject
            {
                ["name"] = name,
                ["description"] = description,
                ["parameters"] = parameters
            }
        };
}
