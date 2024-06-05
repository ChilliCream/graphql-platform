using Microsoft.AspNetCore.Http;

namespace HotChocolate.AspNetCore;

/// <summary>
/// The HTTP request interceptor allows to manipulate the GraphQL
/// request creation and the GraphQL request response creation.
/// </summary>
public interface IHttpRequestInterceptor
{
    /// <summary>
    /// This method is called to build the GraphQL request from the HTTP request.
    /// </summary>
    /// <param name="context">
    /// The <see cref="HttpContext"/>.
    /// </param>
    /// <param name="requestExecutor">
    /// The GraphQL request executor which allows access to the GraphQL schema.
    /// </param>
    /// <param name="requestBuilder">
    /// The GraphQL request builder.
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken"/>.
    /// </param>
    ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken);
}
