using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution;

internal static partial class OperationPlanExecutor
{
    private readonly record struct DeliveryPath(Path PendingPath, int PendingFieldCount);

    private static class DeliveryPathCache
    {
        public static readonly ConditionalWeakTable<OperationPlan, Dictionary<int, DeliveryPath>> Entries = new();
    }
}
