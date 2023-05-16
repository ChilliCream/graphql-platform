namespace HotChocolate.Fusion.Planning;

internal enum QueryPlanNodeKind
{
    Parallel,
    Sequence,
    Resolve,
    ResolveByKeyBatch,
    ResolveNode,
    Subscribe,
    Introspect,
    Compose,
    If
}
