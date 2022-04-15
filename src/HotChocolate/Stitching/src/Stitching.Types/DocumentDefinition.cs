using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types;

internal sealed class DocumentDefinition : ISchemaNode<DocumentNode>
{
    public DocumentDefinition(Func<ISchemaNode, ISchemaCoordinate2> coordinateFactory, DocumentNode documentNode)
    {
        Definition = documentNode;
        Coordinate = coordinateFactory.Invoke(this);
    }

    public DocumentNode Definition { get; private set; }
    public ISchemaNode? Parent => default;
    public ISchemaCoordinate2 Coordinate { get; }

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
