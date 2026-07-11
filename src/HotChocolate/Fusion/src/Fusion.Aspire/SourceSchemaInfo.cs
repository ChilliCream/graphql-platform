using System.Text.Json;

namespace HotChocolate.Fusion.Aspire;

internal sealed record SourceSchemaInfo
{
    public required string Name { get; init; }
    public string? ResourceName { get; init; }
    public Uri? HttpEndpointUrl { get; init; }
    public required SourceSchemaText Schema { get; init; }
    public required JsonDocument SchemaSettings { get; init; }
}
