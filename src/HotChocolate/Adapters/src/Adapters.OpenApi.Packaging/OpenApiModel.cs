namespace HotChocolate.Adapters.OpenApi.Packaging;

/// <summary>
/// Represents an OpenAPI model containing a GraphQL fragment.
/// </summary>
/// <param name="Fragment">The GraphQL fragment as raw bytes.</param>
public sealed record OpenApiModel(ReadOnlyMemory<byte> Fragment);
