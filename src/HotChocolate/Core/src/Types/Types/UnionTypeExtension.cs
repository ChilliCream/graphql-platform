using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class UnionTypeExtension
        : NamedTypeExtensionBase<UnionTypeDefinition>
    {
        private readonly Action<IUnionTypeDescriptor> _configure;

        public UnionTypeExtension()
        {
            _configure = Configure;
        }

        public UnionTypeExtension(Action<IUnionTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.Union;

        protected override UnionTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = UnionTypeDescriptor.New(
                context.DescriptorContext);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IUnionTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            UnionTypeDefinition definition)
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
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is UnionType unionType)
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
                    TypeResources.UnionTypeExtension_CannotMerge);
            }
        }
    }
}
