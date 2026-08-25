namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The timing and concurrency constants the actor-batch foreground
/// dispatcher fixes for one <see cref="ActorWakeDispatcher.DispatchAsync"/>
/// call: the post-commit budget every recipient/target of one claimed batch
/// shares, how many transports run at once, how long the batch's own lease
/// and a per-target session gate are held, and how often the batch lease is
/// renewed while dispatch is in flight. <see cref="BatchLeaseDuration"/> is
/// strictly longer than <see cref="BatchDeadline"/>, so a batch dispatched
/// without contention never has its own lease expire out from under it
/// before the deadline it is itself bounded by.
/// </summary>
internal static class WakeDispatchPolicy
{
    /// <summary>
    /// The absolute budget one <see cref="INotifier.NotifyAsync"/> call
    /// fixes once and shares across every recipient actor and every target
    /// within each of their batches, so a broadcast to many recipients is
    /// never bounded by a multiple of this value.
    /// </summary>
    public static readonly TimeSpan BatchDeadline = TimeSpan.FromSeconds(21);

    /// <summary>
    /// Time reserved at the tail of <see cref="BatchDeadline"/> for
    /// observing a handoff (an access-denied target's offer) rather than
    /// spending it on transport work.
    /// </summary>
    public static readonly TimeSpan HandoffObservationReserve = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// The most transports one batch dispatches at once, matching
    /// <c>ping_leases</c>' four fixed slots.
    /// </summary>
    public const int MaxConcurrentTransports = 4;

    public static readonly TimeSpan BatchLeaseDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How often the dispatcher renews its batch lease while targets are
    /// still in flight. Comfortably shorter than <see cref="BatchLeaseDuration"/>,
    /// so an owner still alive always renews well before its own lease could
    /// expire.
    /// </summary>
    public static readonly TimeSpan BatchRenewInterval = TimeSpan.FromSeconds(7);

    /// <summary>
    /// How long a session ping gate is held for one reserved attempt before
    /// it can be stolen as expired, distinct from the cooldown a successful
    /// attempt extends it to (<see cref="PingPolicy.Cooldown"/>).
    /// </summary>
    public static readonly TimeSpan SessionGateLeaseDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How far out a batch left with durable offered work (busy, cooldown,
    /// capacity, or an access-denied handoff) reschedules its outbox row's
    /// <c>due_at</c>, so a later trigger for the same actor retries it
    /// instead of the offer going stale forever.
    /// </summary>
    public static readonly TimeSpan OfferedRetryDelay = TimeSpan.FromSeconds(30);
}
