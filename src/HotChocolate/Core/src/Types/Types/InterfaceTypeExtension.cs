using System;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class InterfaceTypeExtension
        : NamedTypeExtensionBase<InterfaceTypeDefinition>
    {
        private Action<IInterfaceTypeDescriptor>? _configure;

        protected InterfaceTypeExtension()
        {
            _configure = Configure;
        }

        public InterfaceTypeExtension(
            Action<IInterfaceTypeDescriptor> configure)
        {
            _configure = configure;
        }

        public override TypeKind Kind => TypeKind.Interface;

        protected override InterfaceTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor =
                InterfaceTypeDescriptor.New(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor)
        { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        internal override void Merge(
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is InterfaceType interfaceType)
            {
                TypeExtensionHelper.MergeContextData(
                    Definition,
                    interfaceType.Definition);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition.Directives,
                    interfaceType.Definition.Directives);

                TypeExtensionHelper.MergeInterfaceFields(
                    context,
                    Definition.Fields,
                    interfaceType.Definition.Fields);

                TypeExtensionHelper.MergeConfigurations(
                    Definition.Configurations,
                    interfaceType.Definition.Configurations);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.InterfaceTypeExtension_CannotMerge,
                    nameof(type));
            }
        }
    }
}
