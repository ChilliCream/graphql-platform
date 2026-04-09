using CookieCrumble;

namespace Mocha.Testing;

/// <summary>
/// Snapshot extension methods for <see cref="ITrackedMessages"/> using CookieCrumble.
/// </summary>
public static class TrackedMessagesSnapshotExtensions
{
    /// <summary>
    /// Matches the tracked messages against a Markdown snapshot file.
    /// </summary>
    /// <param name="messages">The tracked messages to snapshot.</param>
    public static void MatchMarkdownSnapshot(this ITrackedMessages messages)
    {
        Snapshot
            .Create()
            .Add(messages, formatter: TrackedMessagesSnapshotValueFormatter.Instance)
            .MatchMarkdown();
    }

    /// <summary>
    /// Matches the tracked messages against an inline snapshot string.
    /// </summary>
    /// <param name="messages">The tracked messages to snapshot.</param>
    /// <param name="expected">The expected snapshot content.</param>
    public static void MatchInlineSnapshot(this ITrackedMessages messages, string expected)
    {
        Snapshot
            .Create()
            .Add(messages, formatter: TrackedMessagesSnapshotValueFormatter.Instance)
            .MatchInline(expected);
    }
}
