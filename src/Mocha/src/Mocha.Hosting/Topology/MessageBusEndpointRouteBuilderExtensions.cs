using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Resources;

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
        "MapMessageBusDeveloperTopology is deprecated. Consume MochaResourceSource directly or expose it via your own HTTP shape. Will be removed in the next major.",
        error: false)]
    public static IEndpointConventionBuilder MapMessageBusDeveloperTopology(
        this IEndpointRouteBuilder endpoints,
        string path = "/.well-known/message-topology")
    {
        return endpoints.MapGet(
            path,
            (HttpContext httpContext) =>
            {
                var source = httpContext.RequestServices.GetRequiredService<MochaMessageBusResourceSource>();
                var description = source.Description;

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
