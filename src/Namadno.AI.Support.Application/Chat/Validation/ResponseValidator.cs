using Namadno.AI.Support.Application.Copy;
using Namadno.AI.Support.Domain.Enums;

namespace Namadno.AI.Support.Application.Chat.Validation;

public interface IResponseValidator
{
    bool IsSafe(string content, ResponseMode mode);
}

public sealed class ResponseValidator(CopyLexicon lexicon) : IResponseValidator
{
    public bool IsSafe(string content, ResponseMode mode)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var lower = content.ToLowerInvariant();
        if (lexicon.ForbiddenResponseNeedles.Any(f => lower.Contains(f, StringComparison.Ordinal)))
        {
            return false;
        }

        _ = mode;
        return true;
    }
}
