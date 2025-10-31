using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Exporters.OpenApi;

internal interface IOpenApiResultFormatter
{
    Task FormatResultAsync(
        IOperationResult operationResult,
        HttpContext httpContext,
        OpenApiEndpointDescriptor endpoint,
        CancellationToken cancellationToken);
}
