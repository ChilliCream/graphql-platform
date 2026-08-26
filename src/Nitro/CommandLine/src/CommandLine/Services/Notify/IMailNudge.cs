namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Tells recipients that new mail is waiting, so an idle agent checks its
/// inbox without waiting for its next turn.
/// </summary>
internal interface IMailNudge
{
    /// <summary>
    /// Nudges every given actor that has a live session with a reachable
    /// endpoint. Actors without one are skipped: they see the mail when they
    /// pull. Never throws; a transport failure is ignored, since the next
    /// turn reports the unread mail anyway.
    /// </summary>
    Task NudgeAsync(IReadOnlyList<string> actors, CancellationToken cancellationToken);
}
