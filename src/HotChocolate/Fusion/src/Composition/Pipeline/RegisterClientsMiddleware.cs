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
                            TransportDirective
                                .CreateHttp(
                                    configuration.Name,
                                    httpClient.BaseAddress,
                                    httpClient.ClientName ?? defaultClientName)
                                .ToDirective(context.FusionTypes));
                        break;

                    case WebSocketClientConfiguration webSocketClient:
                        context.FusionGraph.Directives.Add(
                            TransportDirective
                                .CreateWebsocket(
                                    configuration.Name,
                                    webSocketClient.BaseAddress,
                                    webSocketClient.ClientName ?? defaultClientName)
                                .ToDirective(context.FusionTypes));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(client));
                }
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
