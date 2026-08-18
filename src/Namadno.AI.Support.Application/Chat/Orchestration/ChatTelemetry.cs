using System.Diagnostics.Metrics;

namespace Namadno.AI.Support.Application.Chat.Orchestration;

internal static class ChatTelemetry
{
    private static readonly Meter Meter = new("Namadno.AI.Support");
    private static readonly Counter<long> Messages = Meter.CreateCounter<long>("chat.messages");

    public static void Record(string intent, string mode)
    {
        Messages.Add(
            1,
            new KeyValuePair<string, object?>("intent", intent),
            new KeyValuePair<string, object?>("mode", mode));
    }
}
