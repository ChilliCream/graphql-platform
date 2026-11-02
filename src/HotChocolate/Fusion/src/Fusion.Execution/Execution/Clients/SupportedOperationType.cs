namespace HotChocolate.Fusion.Execution.Clients;

[Flags]
public enum SupportedOperationType
{
    Query = 1 << 0,
    Mutation = 1 << 1,
    Subscription = 1 << 2,
    All = Query | Mutation | Subscription
}
