using System.Runtime.CompilerServices;
namespace HotChocolate.Fusion.Execution.Nodes;

internal static class QueryPlanNodeKindFormatter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(this QueryPlanNodeKind kind)
        => kind switch
        {
            QueryPlanNodeKind.Parallel => nameof(QueryPlanNodeKind.Parallel),
            QueryPlanNodeKind.Sequence => nameof(QueryPlanNodeKind.Sequence),
            QueryPlanNodeKind.Resolve => nameof(QueryPlanNodeKind.Resolve),
            QueryPlanNodeKind.ResolveByKeyBatch => nameof(QueryPlanNodeKind.ResolveByKeyBatch),
            QueryPlanNodeKind.ResolveNode => nameof(QueryPlanNodeKind.ResolveNode),
            QueryPlanNodeKind.Subscribe => nameof(QueryPlanNodeKind.Subscribe),
            QueryPlanNodeKind.Introspect => nameof(QueryPlanNodeKind.Introspect),
            QueryPlanNodeKind.Compose => nameof(QueryPlanNodeKind.Compose),
            QueryPlanNodeKind.If => nameof(QueryPlanNodeKind.If),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
}
