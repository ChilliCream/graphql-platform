using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Types.Extensions;

namespace HotChocolate.Stitching.Types;

internal sealed class DocumentDefinition : ISchemaNode<DocumentNode>
{
    public DocumentDefinition(
        ISchemaCoordinate2 coordinate,
        DocumentNode documentNode)
    {
        Definition = documentNode;
        Coordinate = coordinate;
    }

    public DocumentNode Definition { get; private set; }
    public ISchemaNode? Parent => default;
    public ISchemaCoordinate2 Coordinate { get; }

    public ISchemaNode RewriteDefinition(ISchemaNode original, ISyntaxNode replacement)
    {
        return this;
    }

    public ISchemaNode RewriteDefinition(DocumentNode node)
    {
        Definition = node;

        return this;
    }

    public ISchemaNode RewriteDefinition(IDefinitionNode original, IDefinitionNode node)
    {
        IReadOnlyList<IDefinitionNode> updatedDefinitions = Definition
            .Definitions
            .AddOrReplace(node,
                x => x.Equals(original));

        return RewriteDefinition(Definition
            .WithDefinitions(updatedDefinitions));
    }
}
