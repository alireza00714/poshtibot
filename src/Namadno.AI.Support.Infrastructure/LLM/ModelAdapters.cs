using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions.LLM;
using Namadno.AI.Support.Application.Configuration;

namespace Namadno.AI.Support.Infrastructure.LLM;

internal sealed class MockChatModel : IChatModel
{
    public Task<ChatModelResponse> GenerateAsync(ChatModelRequest request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Mock chat model does not generate free-form text.");
}

internal sealed class ChatModelRouter(IChatModel model, IOptions<LLMOptions> options) : IChatModelRouter
{
    public async Task<ChatModelResponse?> TryGenerateAsync(ChatModelRequest request, CancellationToken cancellationToken)
    {
        if (options.Value.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return await model.GenerateAsync(request, cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

internal sealed class HashEmbeddingModel(IOptions<EmbeddingOptions> options) : IEmbeddingModel
{
    public Task<float[]?> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult<float[]?>(null);
        }

        var dimensions = options.Value.Dimensions;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var vector = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            vector[i] = (hash[i % hash.Length] / 127.5f) - 1f;
        }

        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        if (norm > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }

        return Task.FromResult<float[]?>(vector);
    }
}
