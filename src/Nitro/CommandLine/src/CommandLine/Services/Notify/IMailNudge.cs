namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Tells recipients that new mail is waiting, so an idle agent checks its
/// inbox without waiting for its next turn.
/// </summary>
internal interface IMailNudge
{
    /// <summary>
    /// Nudges every given actor that has a live session with a reachable
    /// endpoint. Actors without one are skipped. Never throws; a transport
    /// failure is ignored.
    /// </summary>
    Task NudgeAsync(IReadOnlyList<string> actors, CancellationToken cancellationToken);
}
