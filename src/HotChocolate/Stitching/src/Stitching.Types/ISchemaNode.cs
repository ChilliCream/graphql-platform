using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

public interface ISchemaNode
{
    ISchemaDatabase Database { get; }
    ISyntaxNode Definition { get; }
    ISchemaNode? Parent { get; }
    ISchemaCoordinate2? Coordinate { get; }
    ISchemaNode RewriteDefinition(ISyntaxNode node);
    ISchemaNode RewriteDefinition(ISchemaNode original, ISyntaxNode replacement);
}

internal interface ISchemaNode<TDefinition> : ISchemaNodeInfo<TDefinition>, ISchemaNode
    where TDefinition : ISyntaxNode
{
    ISyntaxNode ISchemaNode.Definition
    {
        get => Definition;
    }

    ISchemaNode ISchemaNode.RewriteDefinition(ISyntaxNode node)
    {
        return RewriteDefinition((TDefinition)node);
    }

    ISchemaNode RewriteDefinition(TDefinition node);

    /// <summary>
    /// Gets the type definition syntax.
    /// </summary>
    new TDefinition Definition { get; }
}
