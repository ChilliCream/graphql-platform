using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Transport.Http;

public interface IGraphQLHttpClient
{
    public Task<OperationResult> GetAsync(
        OperationRequest request,
        OnHttpRequestMessageCreated? onMessageCreated = null,
        CancellationToken cancellationToken = default);

    public Task<OperationResult> PostAsync(
        OperationRequest request,
        OnHttpRequestMessageCreated? onMessageCreated = null,
        CancellationToken cancellationToken = default);
}

public delegate void OnHttpRequestMessageCreated(
    OperationRequest request, 
    HttpRequestMessage requestMessage);