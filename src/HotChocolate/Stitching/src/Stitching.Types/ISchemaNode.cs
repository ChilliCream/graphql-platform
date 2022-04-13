using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal interface ISchemaNode
{
    ISyntaxNode Definition { get; }
    void RewriteDefinition(ISyntaxNode node);

}

internal interface ISchemaNode<TDefinition> : ISchemaNode
    where TDefinition : ISyntaxNode
{
    ISyntaxNode ISchemaNode.Definition
    {
        get => Definition;
    }

    void ISchemaNode.RewriteDefinition(ISyntaxNode node)
    {
        RewriteDefinition((TDefinition)node);
    }

    void RewriteDefinition(TDefinition node);

    /// <summary>
    /// Gets the type definition syntax.
    /// </summary>
    new TDefinition Definition
    {
        get => (TDefinition)((ISchemaNode)this).Definition;
    }
}
