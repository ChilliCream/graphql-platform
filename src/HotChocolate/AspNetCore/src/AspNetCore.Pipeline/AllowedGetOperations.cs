namespace HotChocolate.AspNetCore;

[Flags]
public enum AllowedGetOperations
{
    None = 0,
    Query = 1,
    Mutation = 2,
    Subscription = 4,
    QueryAndMutation = Query | Mutation,
    All = Query | Mutation | Subscription
}
