using System.Linq;
using System;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Configuration;

namespace HotChocolate.Types
{
    public abstract class NamedTypeBase<TDefinition>
        : TypeBase<TDefinition>
        , INamedType
        , IHasDirectives
        , IHasClrType
        where TDefinition : DefinitionBase, IHasDirectiveDefinition
    {
        public IDirectiveCollection Directives { get; private set; }

        public Type ClrType { get; private set; }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            TDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            ClrType = definition is IHasClrType clr && clr.ClrType != GetType()
                ? clr.ClrType
                : typeof(object);

            context.RegisterDependencyRange(
                definition.Directives.Select(t => t.Reference));
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            TDefinition definition)
        {
            base.OnCompleteType(context, definition);

            var directives = new DirectiveCollection(
                this, definition.Directives);
            directives.CompleteCollection(context);
            Directives = directives;
        }
    }
}
