using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions.LLM;
using Namadno.AI.Support.Application.Configuration;

namespace Namadno.AI.Support.Infrastructure.LLM;

internal sealed class OpenAICompatibleChatModel(IHttpClientFactory httpFactory, IOptions<LLMOptions> options) : IChatModel
{
    public async Task<ChatModelResponse> GenerateAsync(ChatModelRequest request, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var http = httpFactory.CreateClient("llm");
        using var payload = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = settings.Model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                stream = false,
                enable_thinking = settings.EnableThinking,
                chat_template_kwargs = new { enable_thinking = settings.EnableThinking },
                messages = request.Messages.Select(m => new { role = m.Role, content = m.Content })
            }),
            Encoding.UTF8,
            "application/json");

        using var message = new HttpRequestMessage(HttpMethod.Post, Combine(settings.BaseUrl, "chat/completions"))
        {
            Content = payload
        };
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }

        using var response = await http.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Chat completion failed with {(int)response.StatusCode}.");
        }

        if (!OpenAiCompatibleParsers.TryReadChatContent(body, out var content, out var model))
        {
            throw new InvalidOperationException("Chat completion response was malformed.");
        }

        return new ChatModelResponse(content, string.IsNullOrWhiteSpace(model) ? settings.Model : model);
    }

    private static Uri Combine(string baseUrl, string relative) =>
        new(new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute), relative);
}
