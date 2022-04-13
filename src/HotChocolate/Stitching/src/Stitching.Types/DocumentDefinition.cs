using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types;

internal sealed class DocumentDefinition : ISchemaNode<DocumentNode>
{
    public DocumentDefinition(DocumentNode documentNode)
    {
        Definition = documentNode;
    }

    public DocumentNode Definition { get; private set; }

    public void Add(IDefinitionNode definition)
    {
        Definition = Definition
            .WithDefinitions(new [] { definition });
    }

    public void RewriteDefinition(DocumentNode node)
    {
        Definition = node;
    }

    public void RewriteDefinition(IDefinitionNode original, IDefinitionNode node)
    {
        IReadOnlyList<IDefinitionNode> updatedDefinitions = Definition
            .Definitions
            .AddOrReplace(node,
                x => ReferenceEquals(x, original) || x.Equals(original));

        RewriteDefinition(Definition
            .WithDefinitions(updatedDefinitions));
    }
}
