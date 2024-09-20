using HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased.Types;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.CertificationSchema.AnnotationBased;

public static class SchemaSetup
{
    public static async Task<IRequestExecutor> CreateAsync()
        => await new ServiceCollection()
            .AddSingleton<Data>()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();
}
