using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotChocolate.AzureFunctions;

internal sealed class DefaultGraphQLRequestExecutor : IGraphQLRequestExecutor
{
    private readonly EmptyResult _result = new();
    private readonly RequestDelegate _pipeline;
    private readonly GraphQLServerOptions _options;

    public DefaultGraphQLRequestExecutor(RequestDelegate pipeline, GraphQLServerOptions options)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IActionResult> ExecuteAsync(HttpContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // First we need to populate the HttpContext with the current GraphQL server options ...
        context.Items.Add(nameof(GraphQLServerOptions), _options);

        // after that we can execute the pipeline ...
        await _pipeline.Invoke(context).ConfigureAwait(false);

        // last we return out empty result that we have cached in this class.
        // the pipeline actually takes care of writing the result to the
        // response stream.
        return _result;
    }
}
