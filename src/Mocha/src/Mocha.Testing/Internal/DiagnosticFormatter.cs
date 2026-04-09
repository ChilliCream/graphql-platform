using System.Text;

namespace Mocha.Testing.Internal;

/// <summary>
/// Formats tracked events into human-readable diagnostic output.
/// </summary>
internal static class DiagnosticFormatter
{
    /// <summary>
    /// Formats the timeline as an aligned table for diagnostic output.
    /// </summary>
    /// <param name="timeline">The tracked events to format.</param>
    /// <returns>A formatted diagnostic string.</returns>
    public static string FormatTimeline(IReadOnlyList<TrackedEvent> timeline)
    {
        if (timeline.Count == 0)
        {
            return "  (no events)";
        }

        var sb = new StringBuilder();
        var maxKindLength = 0;
        var maxTypeLength = 0;

        for (var i = 0; i < timeline.Count; i++)
        {
            var e = timeline[i];
            var kindName = e.Kind.ToString();
            var typeName = e.MessageType.Name;

            if (kindName.Length > maxKindLength)
            {
                maxKindLength = kindName.Length;
            }

            if (typeName.Length > maxTypeLength)
            {
                maxTypeLength = typeName.Length;
            }
        }

        for (var i = 0; i < timeline.Count; i++)
        {
            var e = timeline[i];
            var kindName = e.Kind.ToString();
            var typeName = e.MessageType.Name;
            var timestamp = e.Timestamp.TotalMilliseconds;

            sb.Append("  ");
            sb.Append(timestamp.ToString("F1").PadLeft(8));
            sb.Append("ms  ");
            sb.Append(kindName.PadRight(maxKindLength));
            sb.Append("  ");
            sb.Append(typeName.PadRight(maxTypeLength));

            if (e.MessageId is not null)
            {
                sb.Append("  (");
                sb.Append(e.MessageId);
                sb.Append(')');
            }

            if (e.Duration is { } duration)
            {
                sb.Append("  [");
                sb.Append(duration.TotalMilliseconds.ToString("F1"));
                sb.Append("ms]");
            }

            if (i < timeline.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a timeout diagnostic message including timeline and pending envelopes.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="timeline">The tracked events timeline.</param>
    /// <param name="pendingKeys">The keys of envelopes that have not completed.</param>
    /// <returns>A formatted diagnostic string.</returns>
    public static string FormatTimeout(
        TimeSpan timeout,
        IReadOnlyList<TrackedEvent> timeline,
        IReadOnlyList<string> pendingKeys)
    {
        var sb = new StringBuilder();
        sb.Append("Mocha message tracking timed out after ");
        sb.Append(timeout.TotalSeconds.ToString("F1"));
        sb.AppendLine("s.");
        sb.AppendLine();
        sb.AppendLine("Timeline:");
        sb.AppendLine(FormatTimeline(timeline));

        if (pendingKeys.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Pending:");
            for (var i = 0; i < pendingKeys.Count; i++)
            {
                sb.Append("  ");
                sb.AppendLine(pendingKeys[i]);
            }
        }

        return sb.ToString();
    }
}
