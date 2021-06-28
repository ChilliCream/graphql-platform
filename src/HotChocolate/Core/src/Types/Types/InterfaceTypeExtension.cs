using System;
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
    public class InterfaceTypeExtension : NamedTypeExtensionBase<InterfaceTypeDefinition>
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

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        protected override void Merge(
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is InterfaceType interfaceType)
            {
                // we first assert that extension and type are mutable and by 
                // this that they do have a type definition.
                AssertMutable();
                interfaceType.AssertMutable();

                TypeExtensionHelper.MergeContextData(
                    Definition!,
                    interfaceType.Definition!);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition!.Directives,
                    interfaceType.Definition!.Directives);

                TypeExtensionHelper.MergeInterfaceFields(
                    context,
                    Definition!.Fields,
                    interfaceType.Definition!.Fields);

                TypeExtensionHelper.MergeConfigurations(
                    Definition!.Configurations,
                    interfaceType.Definition!.Configurations);
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
