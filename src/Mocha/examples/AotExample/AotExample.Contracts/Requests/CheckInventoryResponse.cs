namespace AotExample.Contracts.Requests;

public sealed class CheckInventoryResponse
{
    public required bool IsAvailable { get; init; }
    public required int QuantityOnHand { get; init; }
}
