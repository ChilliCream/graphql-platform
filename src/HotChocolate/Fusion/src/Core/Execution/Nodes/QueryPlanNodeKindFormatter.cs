using System.Runtime.CompilerServices;
namespace HotChocolate.Fusion.Execution.Nodes;

internal static class QueryPlanNodeKindFormatter
{
    private static ReadOnlySpan<byte> Parallel => "Parallel"u8;
    private static ReadOnlySpan<byte> Sequence => "Sequence"u8;
    private static ReadOnlySpan<byte> Resolve => "Resolve"u8;
    private static ReadOnlySpan<byte> ResolveByKeyBatch => "ResolveByKeyBatch"u8;
    private static ReadOnlySpan<byte> ResolveNode => "ResolveNode"u8;
    private static ReadOnlySpan<byte> Subscribe => "Subscribe"u8;
    private static ReadOnlySpan<byte> Introspect => "Introspect"u8;
    private static ReadOnlySpan<byte> Compose => "Compose"u8;
    private static ReadOnlySpan<byte> If => "If"u8;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> Format(this QueryPlanNodeKind kind)
        => kind switch
        {
            QueryPlanNodeKind.Parallel => Parallel,
            QueryPlanNodeKind.Sequence => Sequence,
            QueryPlanNodeKind.Resolve => Resolve,
            QueryPlanNodeKind.ResolveByKeyBatch => ResolveByKeyBatch,
            QueryPlanNodeKind.ResolveNode => ResolveNode,
            QueryPlanNodeKind.Subscribe => Subscribe,
            QueryPlanNodeKind.Introspect => Introspect,
            QueryPlanNodeKind.Compose => Compose,
            QueryPlanNodeKind.If => If,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
}
