using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

internal sealed class GraphQLSourceSchemaAnnotation : IResourceAnnotation
{
    public string? EndpointName { get; init; }

    public string? SchemaPath { get; init; }

    public required SourceSchemaLocationType Location { get; init; }
}
