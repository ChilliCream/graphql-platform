namespace HotChocolate.Fusion.Planning;

internal enum QueryPlanNodeKind
{
    Parallel,
    Serial,
    Fetch,
    Introspection,
    Compose,
}
