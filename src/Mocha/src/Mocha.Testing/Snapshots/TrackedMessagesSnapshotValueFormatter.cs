using System.Buffers;
using CookieCrumble;
using CookieCrumble.Formatters;

namespace Mocha.Testing;

/// <summary>
/// CookieCrumble snapshot value formatter for <see cref="ITrackedMessages"/>.
/// Produces a deterministic markdown table of tracked messages.
/// </summary>
public sealed class TrackedMessagesSnapshotValueFormatter
    : ISnapshotValueFormatter
    , IMarkdownSnapshotValueFormatter
{
    /// <summary>
    /// Gets the singleton instance of the formatter.
    /// </summary>
    public static TrackedMessagesSnapshotValueFormatter Instance { get; } = new();

    /// <inheritdoc />
    public bool CanHandle(object? value) => value is ITrackedMessages;

    /// <inheritdoc />
    public void Format(IBufferWriter<byte> snapshot, object? value)
    {
        if (value is not ITrackedMessages messages)
        {
            return;
        }

        FormatCore(snapshot, messages);
    }

    /// <inheritdoc />
    public void FormatMarkdown(IBufferWriter<byte> snapshot, object? value)
    {
        if (value is not ITrackedMessages messages)
        {
            return;
        }

        FormatCore(snapshot, messages);
    }

    private static void FormatCore(IBufferWriter<byte> snapshot, ITrackedMessages messages)
    {
        snapshot.Append("# Messages");
        snapshot.AppendLine();
        snapshot.AppendLine();

        FormatDispatchedTable(snapshot, messages.Dispatched);
        FormatConsumedTable(snapshot, messages.Consumed);
        FormatFailedTable(snapshot, messages.Failed);
    }

    private static void FormatDispatchedTable(
        IBufferWriter<byte> snapshot,
        IReadOnlyList<TrackedMessage> dispatched)
    {
        snapshot.Append("## Dispatched");
        snapshot.AppendLine();

        if (dispatched.Count == 0)
        {
            snapshot.Append("(none)");
            snapshot.AppendLine();
            snapshot.AppendLine();
            return;
        }

        snapshot.Append("| # | Type | Kind | MessageId |");
        snapshot.AppendLine();
        snapshot.Append("|---|------|------|-----------|");
        snapshot.AppendLine();

        var sorted = SortByTypeName(dispatched);
        for (var i = 0; i < sorted.Length; i++)
        {
            var msg = sorted[i];
            snapshot.Append("| ");
            snapshot.Append((i + 1).ToString());
            snapshot.Append(" | ");
            snapshot.Append(msg.MessageType.Name);
            snapshot.Append(" | ");
            snapshot.Append(msg.DispatchKind.ToString());
            snapshot.Append(" | ");
            snapshot.Append(msg.MessageId ?? "-");
            snapshot.Append(" |");
            snapshot.AppendLine();
        }

        snapshot.AppendLine();
    }

    private static void FormatConsumedTable(
        IBufferWriter<byte> snapshot,
        IReadOnlyList<TrackedMessage> consumed)
    {
        snapshot.Append("## Consumed");
        snapshot.AppendLine();

        if (consumed.Count == 0)
        {
            snapshot.Append("(none)");
            snapshot.AppendLine();
            snapshot.AppendLine();
            return;
        }

        snapshot.Append("| # | Type | MessageId |");
        snapshot.AppendLine();
        snapshot.Append("|---|------|-----------|");
        snapshot.AppendLine();

        var sorted = SortByTypeName(consumed);
        for (var i = 0; i < sorted.Length; i++)
        {
            var msg = sorted[i];
            snapshot.Append("| ");
            snapshot.Append((i + 1).ToString());
            snapshot.Append(" | ");
            snapshot.Append(msg.MessageType.Name);
            snapshot.Append(" | ");
            snapshot.Append(msg.MessageId ?? "-");
            snapshot.Append(" |");
            snapshot.AppendLine();
        }

        snapshot.AppendLine();
    }

    private static void FormatFailedTable(
        IBufferWriter<byte> snapshot,
        IReadOnlyList<TrackedMessage> failed)
    {
        snapshot.Append("## Failed");
        snapshot.AppendLine();

        if (failed.Count == 0)
        {
            snapshot.Append("(none)");
            snapshot.AppendLine();
            snapshot.AppendLine();
            return;
        }

        snapshot.Append("| # | Type | MessageId | Exception |");
        snapshot.AppendLine();
        snapshot.Append("|---|------|-----------|-----------|");
        snapshot.AppendLine();

        var sorted = SortByTypeName(failed);
        for (var i = 0; i < sorted.Length; i++)
        {
            var msg = sorted[i];
            snapshot.Append("| ");
            snapshot.Append((i + 1).ToString());
            snapshot.Append(" | ");
            snapshot.Append(msg.MessageType.Name);
            snapshot.Append(" | ");
            snapshot.Append(msg.MessageId ?? "-");
            snapshot.Append(" | ");
            snapshot.Append(msg.Exception?.GetType().Name ?? "-");
            snapshot.Append(" |");
            snapshot.AppendLine();
        }

        snapshot.AppendLine();
    }

    private static TrackedMessage[] SortByTypeName(IReadOnlyList<TrackedMessage> messages)
    {
        var sorted = new TrackedMessage[messages.Count];
        for (var i = 0; i < messages.Count; i++)
        {
            sorted[i] = messages[i];
        }

        Array.Sort(sorted, static (a, b) =>
            string.Compare(a.MessageType.FullName, b.MessageType.FullName, StringComparison.Ordinal));

        return sorted;
    }
}
