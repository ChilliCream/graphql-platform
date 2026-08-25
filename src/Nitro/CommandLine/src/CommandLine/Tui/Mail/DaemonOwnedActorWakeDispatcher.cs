using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tui.Mail;

/// <summary>
/// An <see cref="IActorWakeDispatcher"/> that never claims or dispatches
/// anything itself. Used by the unified dashboard's mail tab, where an
/// <see cref="IMailWakeDaemonCoordinator"/> already owns dispatch for the
/// whole session: a compose or reply from the board only needs to enqueue
/// the recipient's wake generation (see <see cref="Services.Mail.MailWakePolicy.Enqueue"/>)
/// and observe it through <see cref="IMailWakeReceiptObserver"/>. Running a
/// second, competing dispatch attempt from the same process would only add
/// claim contention for no benefit, since the daemon's own admission loop
/// already claims and dispatches every actor with outstanding wake work.
/// </summary>
internal sealed class DaemonOwnedActorWakeDispatcher : IActorWakeDispatcher
{
    public Task<ActorWakeReceipt?> DispatchAsync(
        string actor, DateTimeOffset deadline, CancellationToken cancellationToken)
        => Task.FromResult<ActorWakeReceipt?>(null);
}
