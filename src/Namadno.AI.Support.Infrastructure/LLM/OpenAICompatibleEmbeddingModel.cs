using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions.LLM;
using Namadno.AI.Support.Application.Configuration;

namespace Namadno.AI.Support.Infrastructure.LLM;

internal sealed class OpenAICompatibleEmbeddingModel(
    IHttpClientFactory httpFactory,
    IOptions<EmbeddingOptions> options,
    ILogger<OpenAICompatibleEmbeddingModel> logger) : IEmbeddingModel
{
    public async Task<float[]?> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            var settings = options.Value;
            var http = httpFactory.CreateClient("embedding");
            using var payload = new StringContent(
                JsonSerializer.Serialize(new { model = settings.Model, input = text }),
                Encoding.UTF8,
                "application/json");
            using var message = new HttpRequestMessage(HttpMethod.Post, Combine(settings.BaseUrl, "embeddings"))
            {
                Content = payload
            };
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            }

            using var response = await http.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Embedding request failed with {StatusCode}", (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!OpenAiCompatibleParsers.TryReadEmbedding(body, settings.Dimensions, out var vector))
            {
                logger.LogWarning("Embedding response was malformed or dimension mismatch.");
                return null;
            }

            return vector;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Embedding request failed; keyword retrieval will continue.");
            return null;
        }
    }

    private static Uri Combine(string baseUrl, string relative) =>
        new(new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute), relative);
}
