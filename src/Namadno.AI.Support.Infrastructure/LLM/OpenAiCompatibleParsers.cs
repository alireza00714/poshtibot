using System.Text.Json;
using System.Text.RegularExpressions;

namespace Namadno.AI.Support.Infrastructure.LLM;

internal static class OpenAiCompatibleParsers
{
    private static readonly Regex ThinkBlock = new(@"<think>[\s\S]*?</think>", RegexOptions.CultureInvariant);
    private static readonly Regex UnclosedThink = new(@"<think>[\s\S]*", RegexOptions.CultureInvariant);

    public static bool TryReadChatContent(string json, out string content, out string model)
    {
        content = string.Empty;
        model = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("model", out var modelElement))
            {
                model = modelElement.GetString() ?? string.Empty;
            }

            if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                return false;
            }

            var first = choices[0];
            if (first.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var messageContent))
            {
                content = StripThinking(messageContent.GetString() ?? string.Empty);
                return !string.IsNullOrWhiteSpace(content);
            }

            if (first.TryGetProperty("text", out var text))
            {
                content = StripThinking(text.GetString() ?? string.Empty);
                return !string.IsNullOrWhiteSpace(content);
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryReadEmbedding(string json, int expectedDimensions, out float[] vector)
    {
        vector = [];
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
            {
                return false;
            }

            if (!data[0].TryGetProperty("embedding", out var embedding) || embedding.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var values = new float[embedding.GetArrayLength()];
            var i = 0;
            foreach (var item in embedding.EnumerateArray())
            {
                values[i++] = item.GetSingle();
            }

            if (expectedDimensions > 0 && values.Length != expectedDimensions)
            {
                return false;
            }

            vector = values;
            return vector.Length > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    internal static string StripThinking(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var stripped = ThinkBlock.Replace(content, string.Empty);
        stripped = UnclosedThink.Replace(stripped, string.Empty);
        return stripped.Trim();
    }
}
