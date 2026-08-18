using Microsoft.Extensions.Options;
using Namadno.AI.Support.Application.Chat.DTOs;
using Namadno.AI.Support.Application.Configuration;

namespace Namadno.AI.Support.Application.Navigation;

public interface INavigationRegistry
{
    bool TryGet(string key, out ChatActionDto action);

    IReadOnlyList<ChatActionDto> WelcomeActions();

    ChatActionDto? ForIntentKey(string? key);
}

public sealed class NavigationRegistry(IOptions<NavigationOptions> options) : INavigationRegistry
{
    public bool TryGet(string key, out ChatActionDto action)
    {
        var match = options.Value.Routes.FirstOrDefault(r =>
            string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));
        if (match is null || !match.Route.StartsWith("app://", StringComparison.Ordinal))
        {
            action = null!;
            return false;
        }

        action = new ChatActionDto("navigation", match.Label, match.Route);
        return true;
    }

    public IReadOnlyList<ChatActionDto> WelcomeActions()
    {
        var keys = new[] { "investment", "leasing", "neobank", "insurance" };
        return keys.Select(key => TryGet(key, out var action) ? action : null)
            .Where(a => a is not null)
            .Cast<ChatActionDto>()
            .ToList();
    }

    public ChatActionDto? ForIntentKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) && TryGet(key, out var action) ? action : null;
}
