using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Mail;

/// <summary>
/// An <see cref="IActorWakeDispatcher"/> whose <see cref="DispatchAsync"/> always throws.
/// </summary>
internal sealed class ThrowingActorWakeDispatcher : IActorWakeDispatcher
{
    public Task<ActorWakeReceipt?> DispatchAsync(
        string actor, string leaderToken, DateTimeOffset deadline, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Dispatch failed.");
}
