namespace HotChocolate.Fusion.Planning;

internal enum QueryPlanNodeKind
{
    Parallel,
    Serial,
    Resolver,
    BatchResolver,
    Subscription,
    Introspection,
    Composition,
}
