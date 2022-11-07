using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;

internal abstract class ApplyExtension<TDef, TExt>
    : IApplyExtension
    where TDef : ITypeDefinitionNode
    where TExt : ITypeExtensionNode
{
    public ITypeDefinitionNode? TryApply(
        ITypeDefinitionNode definition,
        ITypeExtensionNode extension)
        => definition.GetType() == typeof(TDef)
            ? Apply((TDef)definition, (TExt)extension)
            : null;

    protected abstract TDef Apply(TDef definition, TExt extension);
}
