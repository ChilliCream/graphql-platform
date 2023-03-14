namespace HotChocolate.Fusion.Planning;

internal enum QueryPlanNodeKind
{
    Parallel,
    Serial,
    Resolver,
    BatchResolver,
    NodeResolver,
    Subscription,
    Introspection,
    Composition,
}
