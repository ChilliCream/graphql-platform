namespace HotChocolate.Fusion.Planning;

internal enum QueryPlanNodeKind
{
    Parallel,
    Serial,
    Resolver,
    Introspection,
    Composition,
}
