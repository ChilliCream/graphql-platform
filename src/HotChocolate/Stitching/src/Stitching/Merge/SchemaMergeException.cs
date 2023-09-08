using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge;

[Serializable]
public sealed class SchemaMergeException : Exception
{
    public SchemaMergeException(
        ITypeDefinitionNode typeDefinition,
        ITypeExtensionNode typeExtension,
        string message)
        : base(message)
    {
        TypeDefinition = typeDefinition
            ?? throw new ArgumentNullException(nameof(typeDefinition));
        TypeExtension = typeExtension
            ?? throw new ArgumentNullException(nameof(typeExtension));
    }

    public ITypeDefinitionNode TypeDefinition { get; }

    public ITypeExtensionNode TypeExtension { get; }
}
