using HotChocolate.Fusion.Composition.Features;

namespace HotChocolate.Fusion.Composition.Pipeline;

/// <summary>
/// Middleware that adds client configurations for subgraphs with the distributed fusion graph.
/// </summary>
internal sealed class RegisterClientsMiddleware : IMergeMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(CompositionContext context, MergeDelegate next)
    {
        var defaultClientName = context.Features.GetDefaultClientName();

        foreach (var configuration in context.Configurations)
        {
            foreach (var client in configuration.Clients)
            {
                switch (client)
                {
                    case HttpClientConfiguration httpClient:
                        context.FusionGraph.Directives.Add(
                            context.FusionTypes.CreateHttpDirective(
                                configuration.Name,
                                httpClient.ClientName ?? defaultClientName,
                                httpClient.BaseAddress));
                        break;

                    case WebSocketClientConfiguration webSocketClient:
                        context.FusionGraph.Directives.Add(
                            context.FusionTypes.CreateWebSocketDirective(
                                configuration.Name,
                                webSocketClient.ClientName ?? defaultClientName,
                                webSocketClient.BaseAddress));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(client));
                }
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
