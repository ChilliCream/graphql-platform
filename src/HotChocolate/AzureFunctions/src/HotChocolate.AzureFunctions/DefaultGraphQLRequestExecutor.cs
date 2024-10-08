using ChilliCream.Nitro.App;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;

namespace HotChocolate.AzureFunctions;

internal sealed class DefaultGraphQLRequestExecutor : IGraphQLRequestExecutor
{
    private readonly EmptyResult _result = new();
    private readonly RequestDelegate _pipeline;
    private readonly GraphQLServerOptions _options;
    private readonly NitroAppOptions _nitroAppOptions;

    public DefaultGraphQLRequestExecutor(RequestDelegate pipeline, GraphQLServerOptions options)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _nitroAppOptions = _options.Tool.ToNitroAppOptions();
    }

    public async Task<IActionResult> ExecuteAsync(HttpContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        // First we need to populate the HttpContext with the current GraphQL server options ...
        context.Items.Add(nameof(GraphQLServerOptions), _options);
        context.Items.Add(nameof(NitroAppOptions), _nitroAppOptions);

        // after that we can execute the pipeline ...
        await _pipeline.Invoke(context).ConfigureAwait(false);

        // last we return out empty result that we have cached in this class.
        // the pipeline actually takes care of writing the result to the
        // response stream.
        return _result;
    }
}
