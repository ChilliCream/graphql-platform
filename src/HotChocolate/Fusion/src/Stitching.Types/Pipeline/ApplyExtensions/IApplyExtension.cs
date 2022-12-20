using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;

internal interface IApplyExtension
{
    ITypeDefinitionNode? TryApply(
        ITypeDefinitionNode definition,
        ITypeExtensionNode extension);
}
