namespace HotChocolate.AspNetCore;

[Flags]
public enum AllowedGetOperations
{
    None = 0,
    Query = 1,
    [Obsolete("Send mutations over POST. GET requests must not execute mutations.")]
    Mutation = 2,
    Subscription = 4,
    [Obsolete("Use Query instead. GET requests must not execute mutations.")]
    QueryAndMutation = Query | Mutation,
    [Obsolete("Use Query | Subscription instead. GET requests must not execute mutations.")]
    All = Query | Mutation | Subscription
}
