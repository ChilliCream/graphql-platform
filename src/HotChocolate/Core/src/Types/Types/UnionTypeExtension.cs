using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// This is not a full type and is used to split the type configuration into multiple part.
    /// Any type extension instance is will not survive the initialization and instead is
    /// merged into the target type.
    /// </summary>
    public class UnionTypeExtension : NamedTypeExtensionBase<UnionTypeDefinition>
    {
        private Action<IUnionTypeDescriptor>? _configure;

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
            var descriptor =
                UnionTypeDescriptor.New(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

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
                definition.GetDirectives().Select(t => t.TypeReference),
                TypeDependencyKind.Completed);
        }

        protected override void Merge(
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is UnionType unionType)
            {
                // we first assert that extension and type are mutable and by 
                // this that they do have a type definition.
                AssertMutable();
                unionType.AssertMutable();

                TypeExtensionHelper.MergeContextData(
                    Definition!,
                    unionType.Definition!);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition!.Directives,
                    unionType.Definition!.Directives);

                TypeExtensionHelper.MergeTypes(
                    Definition!.Types,
                    unionType.Definition!.Types);

                TypeExtensionHelper.MergeConfigurations(
                    Definition!.Configurations,
                    unionType.Definition!.Configurations);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.UnionTypeExtension_CannotMerge,
                    nameof(type));
            }
        }
    }
}
