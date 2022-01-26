using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.ApolloFederation.CertificationSchema.Types;

namespace HotChocolate.ApolloFederation.CertificationSchema;

public static class SchemaSetup
{
    public static async Task<IRequestExecutor> CreateAsync()
        => await new ServiceCollection()
            .AddSingleton<Data>()
            .AddGraphQL()
            .AddApolloFederation()
            .AddQueryType<Query>()
            .RegisterService<Data>()
            .BuildRequestExecutorAsync();
}
