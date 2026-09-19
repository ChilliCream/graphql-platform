namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The whole result of one <see cref="IActorWakeDispatcher.DispatchAsync"/>
/// call: the actor it dispatched for, its aggregate
/// <see cref="WakeReceiptAggregator"/> status, and every target's own
/// receipt.
/// </summary>
internal sealed record ActorWakeReceipt(
    string Actor, string Status, IReadOnlyList<ActorWakeTargetReceipt> Targets);
