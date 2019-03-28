using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public abstract class TypeBase<TDefinition>
        : TypeSystemObjectBase<TDefinition>
        , IType
        where TDefinition : DefinitionBase
    {
        protected TypeBase() { }

        public abstract TypeKind Kind { get; }
    }
}
