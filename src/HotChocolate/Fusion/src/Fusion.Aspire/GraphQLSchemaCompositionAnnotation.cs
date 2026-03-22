using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

internal sealed class GraphQLSchemaCompositionAnnotation : IResourceAnnotation
{
    public required string OutputFileName { get; init; }

    public required GraphQLCompositionSettings Settings { get; init; }
}
