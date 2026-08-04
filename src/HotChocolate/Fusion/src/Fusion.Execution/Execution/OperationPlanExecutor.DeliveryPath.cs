namespace HotChocolate.Fusion.Execution;

internal static partial class OperationPlanExecutor
{
    private readonly record struct DeliveryPath(Path PendingPath, int PendingFieldCount);
}
