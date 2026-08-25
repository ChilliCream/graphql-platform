namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The direct-first actor-wake state machine: claims one actor's
/// outstanding <c>mail_wake_outbox</c> generation as a frozen batch,
/// dispatches every live claimed session it materialized in the foreground
/// (no detached worker), and settles or reschedules the batch from the
/// targets' own recorded outcomes. See <see cref="ActorWakeDispatcher"/> for
/// the full contract each step follows.
/// </summary>
internal interface IActorWakeDispatcher
{
    /// <summary>
    /// Dispatches <paramref name="actor"/>'s outstanding wake work, or does
    /// nothing and returns null when there is none: no outstanding
    /// generation, its <c>due_at</c> has not arrived, or another owner
    /// already holds a live active batch for this actor. Never throws
    /// except <see cref="OperationCanceledException"/> for
    /// <paramref name="cancellationToken"/> itself; every per-target failure
    /// is recorded, not thrown.
    /// </summary>
    Task<ActorWakeReceipt?> DispatchAsync(string actor, CancellationToken cancellationToken);
}
