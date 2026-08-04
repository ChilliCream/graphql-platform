using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

internal sealed class GraphQLSourceSchemaAnnotation : IResourceAnnotation
{
    public string? SourceSchemaName { get; init; }

    public string? EndpointName { get; init; }

    /// <summary>
    /// The path or download URL of a GraphQL schema document.
    /// </summary>
    public string? SchemaPath { get; init; }

    public required SourceSchemaLocationType Location { get; init; }
}
