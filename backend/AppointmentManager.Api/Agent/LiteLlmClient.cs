using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AppointmentManager.Api.Agent;

public class LiteLlmOptions
{
    public string BaseUrl { get; set; } = "http://localhost:4000";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
}

/// Thin client for LiteLLM's OpenAI-compatible /chat/completions endpoint,
/// including tool/function calling.
public class LiteLlmClient
{
    private readonly HttpClient _http;
    private readonly LiteLlmOptions _options;

    public LiteLlmClient(HttpClient http, LiteLlmOptions options)
    {
        _http = http;
        _options = options;
        // Trailing slash matters: HttpClient resolves a relative request URI against
        // BaseAddress per RFC 3986, which drops any path on BaseAddress (e.g. "/v1")
        // unless BaseAddress itself ends in "/".
        var baseUrl = _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
        _http.BaseAddress = new Uri(baseUrl);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }
    }

    public async Task<JsonObject> CreateChatCompletionAsync(JsonArray messages, JsonArray tools, CancellationToken ct = default)
    {
        // Callers reuse the same messages/tools nodes across repeated calls (the
        // orchestrator's tool-calling loop), but a JsonNode can only ever belong to
        // one parent — so each request gets its own deep-cloned copies to attach.
        var payload = new JsonObject
        {
            ["model"] = _options.Model,
            ["messages"] = messages.DeepClone(),
            ["tools"] = tools.DeepClone(),
            ["tool_choice"] = "auto"
        };

        using var content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("chat/completions", content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"LiteLLM request failed ({(int)response.StatusCode}): {body}");

        var json = JsonNode.Parse(body)?.AsObject()
            ?? throw new InvalidOperationException("LiteLLM returned an empty response.");
        return json;
    }
}
