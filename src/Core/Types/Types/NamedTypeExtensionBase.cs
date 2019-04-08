using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public abstract class NamedTypeExtensionBase<TDefinition>
        : TypeSystemObjectBase<TDefinition>
        , INamedTypeExtension
        where TDefinition : DefinitionBase, IHasDirectiveDefinition
    {
    }
}
