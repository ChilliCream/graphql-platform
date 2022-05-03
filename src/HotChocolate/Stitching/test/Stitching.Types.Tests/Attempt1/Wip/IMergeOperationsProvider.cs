using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt1.Wip;

public interface IMergeOperationsProvider
{
    void Apply(SubgraphDocument sourceDefinition, ISchemaNode destinationDefinition);
    void Apply(ISyntaxNode source, ISchemaNode destination);
}
