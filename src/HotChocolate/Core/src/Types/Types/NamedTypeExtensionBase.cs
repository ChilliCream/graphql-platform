using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public abstract class NamedTypeExtensionBase<TDefinition>
        : TypeSystemObjectBase<TDefinition>
        , INamedTypeExtension
        , INamedTypeExtensionMerger
        where TDefinition : DefinitionBase, IHasDirectiveDefinition
    {
        public abstract TypeKind Kind { get; }

        internal abstract void Merge(
            ITypeCompletionContext context,
            INamedType type);

        void INamedTypeExtensionMerger.Merge(
            ITypeCompletionContext context,
            INamedType type) => Merge(context, type);
    }
}
