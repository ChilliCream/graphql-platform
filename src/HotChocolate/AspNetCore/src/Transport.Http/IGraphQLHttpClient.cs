using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Transport.Serialization;

namespace HotChocolate.Transport.Http;

public interface IGraphQLHttpClient
{
    public Task<GraphQLHttpResponse> ExecuteAsync(
        GraphQLHttpRequest request,
        CancellationToken cancellationToken = default);
}