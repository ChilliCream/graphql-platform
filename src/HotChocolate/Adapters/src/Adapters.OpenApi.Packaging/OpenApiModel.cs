using System.Text.Json;

namespace HotChocolate.Adapters.OpenApi.Packaging;

/// <summary>
/// Represents an OpenAPI model containing the GraphQL document and settings.
/// </summary>
/// <param name="Document">The GraphQL document as raw bytes.</param>
/// <param name="Settings">The settings document for this model.</param>
public sealed record OpenApiModel(
    ReadOnlyMemory<byte> Document,
    JsonDocument Settings) : IDisposable
{
    /// <summary>
    /// Releases the resources used by the model.
    /// </summary>
    public void Dispose()
    {
        Settings.Dispose();
    }
}
