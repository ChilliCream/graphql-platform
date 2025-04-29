using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Execution.Clients;

public interface IGraphQLHttpClientScope
{
    GraphQLHttpClient GetClient(string name);
}
