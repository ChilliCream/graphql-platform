using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public abstract class NamedTypeBase<TDefinition>
        : TypeBase<TDefinition>
        , INamedType
        , IHasDirectives
        , IHasClrType
        where TDefinition : DefinitionBase
    {
        public IDirectiveCollection Directives { get; private set; }

        public Type ClrType { get; private set; }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            TDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            ClrType = definition is IHasClrType clr
                ? clr.ClrType
                : typeof(object);
        }
    }
}
