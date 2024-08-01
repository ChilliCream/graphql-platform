using HotChocolate.ApolloFederation.CertificationSchema.CodeFirst.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.CertificationSchema.CodeFirst;

public static class SchemaSetup
{
    public static async Task<IRequestExecutor> CreateAsync()
        => await new ServiceCollection()
            .AddSingleton<Data>()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<QueryType>()
            .BuildRequestExecutorAsync();
}
