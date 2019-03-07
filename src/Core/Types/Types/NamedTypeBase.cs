using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public abstract class NamedTypeBase<TDefinition>
        : TypeBase<TDefinition>
        , INamedType
        , IHasDirectives
        where TDefinition : DefinitionBase
    {
        public IDirectiveCollection Directives { get; private set; }
    }
}
