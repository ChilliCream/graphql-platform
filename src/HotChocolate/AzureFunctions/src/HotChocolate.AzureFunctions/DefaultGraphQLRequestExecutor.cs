using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.AzureFunctions;

internal sealed class DefaultGraphQLRequestExecutor : IGraphQLRequestExecutor
{
    private readonly EmptyResult _result = new();
    private readonly RequestDelegate _pipeline;

    public DefaultGraphQLRequestExecutor(RequestDelegate pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public async Task<IActionResult> ExecuteAsync(HttpRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        await _pipeline.Invoke(request.HttpContext).ConfigureAwait(false);
        return _result;
    }
}
