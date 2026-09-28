namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// Timing limits for wake dispatch, batch renewal, session reservations, and retries.
/// </summary>
internal static class WakeDispatchPolicy
{
    /// <summary>
    /// The duration callers use to set an actor dispatch's deadline.
    /// </summary>
    public static readonly TimeSpan BatchDeadline = TimeSpan.FromSeconds(21);

    /// <summary>
    /// Time excluded from transport work at the end of a batch's dispatch budget.
    /// </summary>
    public static readonly TimeSpan HandoffObservationReserve = TimeSpan.FromMilliseconds(500);

    public static readonly TimeSpan BatchLeaseDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The interval between batch lease renewals during dispatch.
    /// </summary>
    public static readonly TimeSpan BatchRenewInterval = TimeSpan.FromSeconds(7);

    /// <summary>
    /// The duration of a session gate reservation before expiry.
    /// </summary>
    public static readonly TimeSpan SessionGateLeaseDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The delay before retrying a batch with pending work.
    /// </summary>
    public static readonly TimeSpan OfferedRetryDelay = TimeSpan.FromSeconds(30);
}
