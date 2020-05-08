using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class InputUnionTypeExtension
        : NamedTypeExtensionBase<InputUnionTypeDefinition>
    {
        private readonly Action<IInputUnionTypeDescriptor> _configure;

        public InputUnionTypeExtension()
        {
            _configure = Configure;
        }

        public InputUnionTypeExtension(Action<IInputUnionTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.InputUnion;

        protected override InputUnionTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = InputUnionTypeDescriptor.New(
                context.DescriptorContext);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInputUnionTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            InputUnionTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);

            context.RegisterDependencyRange(
                definition.Types,
                TypeDependencyKind.Default);

            context.RegisterDependencyRange(
                definition.Directives.Select(t => t.TypeReference),
                TypeDependencyKind.Completed);
        }

        internal override void Merge(
            ICompletionContext context,
            INamedType type)
        {
            if (type is InputUnionType unionType)
            {
                TypeExtensionHelper.MergeContextData(
                    Definition,
                    unionType.Definition);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition.Directives,
                    unionType.Definition.Directives);

                TypeExtensionHelper.MergeTypes(
                    Definition.Types,
                    unionType.Definition.Types);

                TypeExtensionHelper.MergeConfigurations(
                    Definition.Configurations,
                    unionType.Definition.Configurations);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.InputUnionTypeExtension_CannotMerge);
            }
        }
    }
}
