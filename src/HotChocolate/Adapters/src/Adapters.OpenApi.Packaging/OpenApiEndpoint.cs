using System.Text.Json;

namespace HotChocolate.Adapters.OpenApi.Packaging;

/// <summary>
/// Represents an OpenAPI endpoint containing the GraphQL document and settings.
/// </summary>
/// <param name="Document">The GraphQL document as raw bytes.</param>
/// <param name="Settings">The settings document for this endpoint.</param>
public sealed record OpenApiEndpoint(
    ReadOnlyMemory<byte> Document,
    JsonDocument Settings) : IDisposable
{
    /// <summary>
    /// Releases the resources used by the endpoint.
    /// </summary>
    public void Dispose()
    {
        Settings.Dispose();
    }
}
