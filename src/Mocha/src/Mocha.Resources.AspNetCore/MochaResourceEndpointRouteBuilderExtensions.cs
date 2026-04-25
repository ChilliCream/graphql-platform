using System.Buffers;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Mocha.Resources.AspNetCore;

/// <summary>
/// Provides extension methods for exposing the composite <see cref="MochaResourceSource"/>
/// over an HTTP endpoint.
/// </summary>
/// <remarks>
/// <para>
/// This package is a convenience adapter for consumers that want a standard HTTP shape over the
/// resource source. The mapped endpoint is auth-agnostic — the caller is expected to chain
/// <c>RequireAuthorization(...)</c> (or the equivalent) to gate access. The wire shape is the
/// minimal transport-neutral envelope described by the <c>Mocha.Resources</c> design: each
/// resource is serialised as <c>{ "kind": ..., "id": ..., "attributes": { ... } }</c>, with the
/// attributes object body produced by <see cref="MochaResource.Write(Utf8JsonWriter)"/>.
/// </para>
/// <para>
/// The endpoint emits an <c>ETag</c> response header derived from the source's current change
/// token. Clients may pass that value back as the <c>If-None-Match</c> request header (or the
/// query string parameter <c>since</c>) to receive a <c>304 Not Modified</c> when the snapshot
/// hasn't changed; this is a long-poll-friendly signal that consumers can adopt without extra
/// plumbing on the producer side.
/// </para>
/// </remarks>
public static class MochaResourceEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps an HTTP GET endpoint that serialises the composite <see cref="MochaResourceSource"/>
    /// as a flat JSON document.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <param name="path">The URL path for the resource endpoint. Defaults to <c>/.well-known/mocha-resources</c>.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> for further endpoint configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="endpoints"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the resource source is not available in the service provider.</exception>
    public static IEndpointConventionBuilder MapMochaResourceEndpoint(
        this IEndpointRouteBuilder endpoints,
        string path = "/.well-known/mocha-resources")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return endpoints.MapGet(path, HandleAsync);
    }

    private static async Task HandleAsync(HttpContext httpContext)
    {
        var source =
            httpContext.RequestServices.GetService<MochaResourceSource>()
            ?? throw new InvalidOperationException(
                "MochaResourceSource is not registered. Call services.AddMochaResources() and at least one source contributor.");

        // Capture the change token *before* the snapshot — otherwise a change that fires between
        // the two reads is missed. This mirrors the consumer rule on MochaResourceSource.
        var changeToken = source.GetChangeToken();
        var resources = source.Resources;

        var etag = ComputeEtag(changeToken);
        httpContext.Response.Headers.ETag = etag;
        httpContext.Response.Headers.CacheControl = "no-store";

        if (TryReadIfNoneMatch(httpContext, out var ifNoneMatch)
            && string.Equals(ifNoneMatch, etag, StringComparison.Ordinal)
            && !changeToken.HasChanged)
        {
            httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        httpContext.Response.ContentType = "application/json";

        var bufferWriter = new ArrayBufferWriter<byte>();
        await using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("resources");

            foreach (var resource in resources)
            {
                writer.WriteStartObject();
                writer.WriteString("kind", resource.Kind);
                writer.WriteString("id", resource.Id);
                writer.WriteStartObject("attributes");
                resource.Write(writer);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        await httpContext.Response.Body.WriteAsync(bufferWriter.WrittenMemory, httpContext.RequestAborted);
    }

    private static bool TryReadIfNoneMatch(HttpContext httpContext, out string value)
    {
        var headerValue = httpContext.Request.Headers.IfNoneMatch;
        if (!StringValues.IsNullOrEmpty(headerValue))
        {
            value = headerValue.ToString();
            return true;
        }

        if (httpContext.Request.Query.TryGetValue("since", out var queryValue)
            && !StringValues.IsNullOrEmpty(queryValue))
        {
            value = queryValue.ToString();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string ComputeEtag(IChangeToken changeToken)
    {
        // The token instance identity is the natural version stamp — every call to GetChangeToken()
        // either returns the same token (no change) or a fresh instance (change). HashCode is good
        // enough as a monotonic-ish discriminator for the long-poll/conditional-GET hint.
        var hash = (uint)changeToken.GetHashCode();
        return string.Concat("\"", hash.ToString("x8", CultureInfo.InvariantCulture), "\"");
    }
}
