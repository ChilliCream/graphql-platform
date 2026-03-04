using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Adapters.OpenApi;

internal interface IOpenApiResultFormatter
{
    Task FormatResultAsync(
        OperationResult operationResult,
        HttpContext httpContext,
        OpenApiEndpointDescriptor endpoint,
        CancellationToken cancellationToken);
}
