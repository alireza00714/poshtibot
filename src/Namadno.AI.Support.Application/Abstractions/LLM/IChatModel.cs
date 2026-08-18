namespace Namadno.AI.Support.Application.Abstractions.LLM;

public sealed record ChatModelMessage(string Role, string Content);

public sealed record ChatModelRequest(
    IReadOnlyList<ChatModelMessage> Messages,
    int MaxTokens,
    double Temperature = 0.2);

public sealed record ChatModelResponse(string Content, string Model);

public interface IChatModel
{
    Task<ChatModelResponse> GenerateAsync(ChatModelRequest request, CancellationToken cancellationToken);
}

public interface IChatModelRouter
{
    Task<ChatModelResponse?> TryGenerateAsync(ChatModelRequest request, CancellationToken cancellationToken);
}

public interface IEmbeddingModel
{
    Task<float[]?> EmbedAsync(string text, CancellationToken cancellationToken);
}
