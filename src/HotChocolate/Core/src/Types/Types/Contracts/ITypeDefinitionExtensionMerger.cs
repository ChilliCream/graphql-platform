using HotChocolate.Configuration;

namespace HotChocolate.Types;

/// <summary>
/// This internal interface is used by the type initialization to
/// merge the type extension into the actual type.
/// </summary>
internal interface ITypeDefinitionExtensionMerger : ITypeDefinitionExtension
{
    /// <summary>
    /// Gets the type extended by this type extension.
    /// </summary>
    Type? ExtendsType { get; }

    /// <summary>
    /// The merge method that allows to merge the type extension into the named type.
    /// </summary>
    /// <param name="context">The type extension completion context.</param>
    /// <param name="type">The target type into which we merge the type extension.</param>
    void Merge(ITypeCompletionContext context, ITypeDefinition type);
}
