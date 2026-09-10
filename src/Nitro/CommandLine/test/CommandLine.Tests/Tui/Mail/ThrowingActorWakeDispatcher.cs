using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// An <see cref="IActorWakeDispatcher"/> that always throws, standing in for
/// a foreground dispatcher failing after a message already committed.
/// Exercises <see cref="ChilliCream.Nitro.CommandLine.Tui.Mail.MailMode"/>'s
/// own catch around the dispatch-and-observe step: a failure here must never
/// be reported as unsent, only as an outcome-unknown reconciliation.
/// </summary>
internal sealed class ThrowingActorWakeDispatcher : IActorWakeDispatcher
{
    public Task<ActorWakeReceipt?> DispatchAsync(string actor, DateTimeOffset deadline, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Dispatch failed.");
}
