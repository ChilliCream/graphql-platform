using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Tests.Agents;

internal sealed class FakeActorWakeDispatcher : IActorWakeDispatcher
{
    public List<string> DispatchedActors { get; } = [];

    public List<DateTimeOffset> ReceivedDeadlines { get; } = [];

    public string? ThrowingActor { get; set; }

    public Task<ActorWakeReceipt?> DispatchAsync(
        string actor, DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        DispatchedActors.Add(actor);
        ReceivedDeadlines.Add(deadline);

        if (actor == ThrowingActor)
        {
            throw new InvalidOperationException($"Simulated dispatch failure for '{actor}'.");
        }

        return Task.FromResult<ActorWakeReceipt?>(new ActorWakeReceipt(actor, MailWakeTargetStatus.Delivered, []));
    }
}
