namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Dispatches one actor's outstanding wake work to at most one coding session
/// and settles or reschedules the claimed batch.
/// </summary>
internal interface IActorWakeDispatcher
{
    /// <summary>
    /// Dispatches the actor's pending wake work within the supplied transport deadline,
    /// fencing every batch write on the caller's leader lease token, and returns null
    /// when no batch can be claimed. Transport outcomes are recorded without propagating
    /// transport failures; caller cancellation and workspace or storage failures propagate.
    /// </summary>
    Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken);
}
