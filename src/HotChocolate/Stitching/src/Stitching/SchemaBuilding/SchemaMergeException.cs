using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.SchemaBuilding;

[Serializable]
public class SchemaMergeException : Exception
{
    public SchemaMergeException(
        ITypeDefinitionNode typeDefinition,
        ITypeExtensionNode typeExtension,
        string message)
        : base(message)
    {
        TypeDefinition = typeDefinition ??
            throw new ArgumentNullException(nameof(typeDefinition));
        TypeExtension = typeExtension ??
            throw new ArgumentNullException(nameof(typeExtension));
    }

    public ITypeDefinitionNode TypeDefinition { get; }

    public ITypeExtensionNode TypeExtension { get; }
}
