namespace ChilliCream.Nitro.CommandLine.Services.Notify;

/// <summary>
/// An actor's wake status and the receipts for the targets considered by its dispatch.
/// </summary>
internal sealed record ActorWakeReceipt(
    string Actor, string Status, IReadOnlyList<ActorWakeTargetReceipt> Targets);
