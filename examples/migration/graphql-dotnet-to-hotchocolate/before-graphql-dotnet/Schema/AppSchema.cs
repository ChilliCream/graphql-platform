using GraphQL.Types;

namespace BeforeGraphQLDotNet.Schema;

public sealed class AppSchema : GraphQL.Types.Schema
{
    public AppSchema(IServiceProvider provider, Query query, Mutation mutation, Subscription subscription)
        : base(provider)
    {
        Query = query;
        Mutation = mutation;
        Subscription = subscription;
    }
}
