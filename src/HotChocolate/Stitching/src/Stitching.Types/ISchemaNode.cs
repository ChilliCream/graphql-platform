using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal interface ISchemaNode
{
    ISyntaxNode Definition { get; set; }
}

internal interface ISchemaNode<TDefinition> : ISchemaNode
    where TDefinition : ISyntaxNode
{
    ISyntaxNode ISchemaNode.Definition
    {
        get => Definition;
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets the type definition syntax.
    /// </summary>
    new TDefinition Definition
    {
        get => (TDefinition)((ISchemaNode)this).Definition;
        set => ((ISchemaNode)this).Definition = value;
    }
}
