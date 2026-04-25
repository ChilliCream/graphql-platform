using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mocha.Hosting;

/// <summary>
/// Provides extension methods for exposing message bus topology information as HTTP endpoints.
/// </summary>
public static class MessageBusEndpointRouteBuilderExtensions
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Maps an HTTP GET endpoint that exposes the message bus topology as JSON for diagnostic purposes.
    /// This endpoint reveals internal routing, consumer, and endpoint details and should only be
    /// used during development - similar to <c>UseDeveloperExceptionPage</c>.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <param name="path">The URL path for the topology endpoint. Defaults to <c>/.well-known/message-topology</c>.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for further endpoint configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the messaging runtime is not available in the service provider.</exception>
    [Obsolete(
        "MapMessageBusDeveloperTopology is deprecated. Use Mocha.Resources.AspNetCore.MapMochaResourceEndpoint or a custom MochaResourceSource consumer. See https://chillicream.com/docs/mocha/resources for migration. Will be removed in the next major.",
        error: false)]
    public static IEndpointConventionBuilder MapMessageBusDeveloperTopology(
        this IEndpointRouteBuilder endpoints,
        string path = "/.well-known/message-topology")
    {
        return endpoints.MapGet(
            path,
            (HttpContext httpContext) =>
            {
                // Route through MochaMessageBusResourceSource so the bridge consumes the same
                // visitor-cached description tree that feeds the resource snapshot. Falls back to
                // an inline visitor pass if the source isn't registered (preserves the pre-resources
                // behaviour for callers that haven't migrated their DI yet).
                var description = ResolveDescription(httpContext);

                // Reshape into the DiagramData format expected by the visualizer:
                // { services: [{ host, messageTypes, consumers, routes, sagas }], transports: [...] }
                var diagramData = new DiagramDataPayload(
                    [
                        new ServicePayload(
                            description.Host,
                            description.MessageTypes,
                            description.Consumers,
                            description.Routes,
                            description.Sagas ?? [])
                    ],
                    description.Transports);

                return Results.Content(
                    JsonSerializer.Serialize(diagramData, s_jsonOptions),
                    "application/json");
            });
    }

    private static MessageBusDescription ResolveDescription(HttpContext httpContext)
    {
        var source = httpContext.RequestServices.GetService<MochaMessageBusResourceSource>();
        if (source is not null)
        {
            return source.Description;
        }

        var runtime =
            httpContext.RequestServices.GetRequiredService<IMessagingRuntime>() as MessagingRuntime
            ?? throw new InvalidOperationException("Message bus runtime is not available.");

        return MessageBusDescriptionVisitor.Visit(runtime);
    }

    private sealed record DiagramDataPayload(
        IReadOnlyList<ServicePayload> Services,
        IReadOnlyList<TransportDescription> Transports);

    private sealed record ServicePayload(
        HostDescription Host,
        IReadOnlyList<MessageTypeDescription> MessageTypes,
        IReadOnlyList<ConsumerDescription> Consumers,
        RoutesDescription Routes,
        IReadOnlyList<SagaDescription> Sagas);
}
