using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

internal sealed class GraphQLSourceSchemaAnnotation : IResourceAnnotation
{
    public string? SourceSchemaName { get; init; }

    public string? EndpointName { get; init; }

    public string? SchemaPath { get; init; }

    public string? GraphQLPath { get; init; }

    public required SourceSchemaLocationType Location { get; init; }
}
