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
    /// Maps an HTTP GET endpoint that returns the message bus topology as JSON.
    /// </summary>
    /// <remarks>
    /// The endpoint serializes the runtime topology description (routes, consumers, and endpoints)
    /// using camelCase JSON naming. It requires the <see cref="IMessagingRuntime"/> to be registered
    /// as a <c>MessagingRuntime</c> instance in the service provider.
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <param name="path">The URL path for the topology endpoint. Defaults to <c>/.well-known/message-topology</c>.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for further endpoint configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the messaging runtime is not available in the service provider.</exception>
    public static IEndpointConventionBuilder MapMessageBus(
        this IEndpointRouteBuilder endpoints,
        string path = "/.well-known/message-topology")
    {
        return endpoints.MapGet(
            path,
            (HttpContext httpContext) =>
            {
                var runtime =
                    httpContext.RequestServices.GetRequiredService<IMessagingRuntime>() as MessagingRuntime
                    ?? throw new InvalidOperationException("Message bus runtime is not available.");

                var description = MessageBusDescriptionVisitor.Visit(runtime);

                return Results.Content(
                    JsonSerializer.Serialize<MessageBusDescription>(description, s_jsonOptions),
                    "application/json");
            });
    }
}
