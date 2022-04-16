using HotChocolate.Language;

namespace HotChocolate.Stitching.Types;

internal interface ISchemaNodeInfo<out TDefinition>
    where TDefinition : ISyntaxNode
{
    /// <summary>
    /// Gets the type definition syntax.
    /// </summary>
    TDefinition Definition { get; }
}