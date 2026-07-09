using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.AzureFunctions;

internal sealed class DefaultGraphQLRequestExecutor : IGraphQLRequestExecutor
{
    private readonly EmptyResult _result = new();
    private readonly RequestDelegate _pipeline;

    public DefaultGraphQLRequestExecutor(RequestDelegate pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);

        _pipeline = pipeline;
    }

    public async Task<IActionResult> ExecuteAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _pipeline.Invoke(context).ConfigureAwait(false);

        return _result;
    }
}
