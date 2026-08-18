using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Abstractions.Integrations;
using Namadno.AI.Support.Application.Configuration;
using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Application.Tools;

namespace Namadno.AI.Support.Infrastructure.Integrations.Namadno;

internal sealed class NamadnoHttpGateway(
    IHttpClientFactory httpFactory,
    IUserContext user,
    IOptions<NamadnoIntegrationOptions> options,
    CopyTexts copy,
    ILogger<NamadnoHttpGateway> logger)
{
    public async Task<ToolExecutionResult> GetAsync(string relativePath, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var settings = options.Value;
            var http = httpFactory.CreateClient("namadno");
            var uri = BuildUri(settings.BaseUrl, relativePath, userId);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", user.CorrelationId);
            request.Headers.TryAddWithoutValidation("X-User-Id", userId);

            using var response = await http.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ToolExecutionResult(true, copy.Tools.NamadnoNotFound, null);
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Namadno GET {Path} failed with {StatusCode}", relativePath, (int)response.StatusCode);
                return new ToolExecutionResult(false, string.Empty, "EXTERNAL_SERVICE_UNAVAILABLE");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ToolExecutionResult(
                true,
                NamadnoResponseMapper.Map(
                    body,
                    copy.Tools.NamadnoStatusFallback),
                null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            logger.LogWarning(ex, "Namadno GET {Path} failed", relativePath);
            return new ToolExecutionResult(false, string.Empty, "EXTERNAL_SERVICE_UNAVAILABLE");
        }
    }

    private static Uri BuildUri(string baseUrl, string relativePath, string userId)
    {
        var builder = new UriBuilder(new Uri(new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute), relativePath.TrimStart('/')));
        var existing = builder.Query.TrimStart('?');
        var userQuery = "userId=" + Uri.EscapeDataString(userId);
        builder.Query = string.IsNullOrEmpty(existing) ? userQuery : existing + "&" + userQuery;
        return builder.Uri;
    }
}

internal static class NamadnoResponseMapper
{
    public static string Map(string body, string fallbackMessage)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return fallbackMessage;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var candidate = FirstString(document.RootElement, "message", "summary", "statusText", "description");
            if (!string.IsNullOrWhiteSpace(candidate) && IsSafeForUser(candidate))
            {
                return candidate.Trim();
            }
        }
        catch (JsonException)
        {
            // Non-JSON bodies are not forwarded; they may contain internals.
        }

        return fallbackMessage;
    }

    private static string? FirstString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text) && text.Length <= 500)
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static bool IsSafeForUser(string text)
    {
        var lower = text.ToLowerInvariant();
        return !lower.Contains("http://", StringComparison.Ordinal)
               && !lower.Contains("https://", StringComparison.Ordinal)
               && !lower.Contains("ignore previous", StringComparison.Ordinal)
               && !lower.Contains("system prompt", StringComparison.Ordinal);
    }
}

internal sealed class HttpInvestmentService(NamadnoHttpGateway gateway, IOptions<NamadnoIntegrationOptions> options) : IInvestmentService
{
    public Task<ToolExecutionResult> GetFundOrderStatusAsync(string userId, CancellationToken cancellationToken) =>
        gateway.GetAsync(options.Value.Apis.FundOrderStatus, userId, cancellationToken);
}

internal sealed class HttpNeobankService(NamadnoHttpGateway gateway, IOptions<NamadnoIntegrationOptions> options) : INeobankService
{
    public Task<ToolExecutionResult> GetTransferStatusAsync(string userId, CancellationToken cancellationToken) =>
        gateway.GetAsync(options.Value.Apis.TransferStatus, userId, cancellationToken);

    public Task<ToolExecutionResult> GetCardStatusAsync(string userId, CancellationToken cancellationToken) =>
        gateway.GetAsync(options.Value.Apis.CardStatus, userId, cancellationToken);
}

internal sealed class HttpInsuranceService(NamadnoHttpGateway gateway, IOptions<NamadnoIntegrationOptions> options) : IInsuranceService
{
    public Task<ToolExecutionResult> GetPolicySummaryAsync(string userId, CancellationToken cancellationToken) =>
        gateway.GetAsync(options.Value.Apis.InsurancePolicy, userId, cancellationToken);
}

internal sealed class HttpPaymentService(NamadnoHttpGateway gateway, IOptions<NamadnoIntegrationOptions> options) : IPaymentService
{
    public Task<ToolExecutionResult> GetLatestTransactionAsync(string userId, CancellationToken cancellationToken) =>
        gateway.GetAsync(options.Value.Apis.PaymentStatus, userId, cancellationToken);
}

internal sealed class HttpUserService(NamadnoHttpGateway gateway, IOptions<NamadnoIntegrationOptions> options) : IUserService
{
    public Task<ToolExecutionResult> GetProfileAsync(string userId, CancellationToken cancellationToken) =>
        gateway.GetAsync(options.Value.Apis.UserProfile, userId, cancellationToken);
}
