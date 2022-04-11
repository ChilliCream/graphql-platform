using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types;

/// <summary>
/// A type definition holds the type definition syntax as well as metadata about the type.
/// </summary>
internal interface ITypeDefinition<TDefinition> : ISchemaNode<TDefinition>
    where TDefinition : IDefinitionNode
{
    /// <summary>
    /// Gets the name of the type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Specifies the type kind this definition represents.
    /// </summary>
    /// <value></value>
    TypeKind Kind { get; }

    /// <summary>
    /// Defines if the specified definition is a type extension.
    /// </summary>
    bool IsExtension { get; }
}
