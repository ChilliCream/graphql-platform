namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// The whole result of one <see cref="IActorWakeDispatcher.DispatchAsync"/>
/// call: the actor it dispatched for, its aggregate
/// <see cref="WakeReceiptAggregator"/> status, and every target's own
/// receipt. Not surfaced to any command yet (no CLI or TUI rendering exists
/// for it): its purpose in this bead is to make the direct-first state
/// machine's truthful outcome observable and testable in-process.
/// </summary>
internal sealed record ActorWakeReceipt(
    string Actor, string Status, IReadOnlyList<ActorWakeTargetReceipt> Targets);
