using System;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public class ObjectTypeExtension
        : NamedTypeExtensionBase<ObjectTypeDefinition>
    {
        private Action<IObjectTypeDescriptor>? _configure;

        protected ObjectTypeExtension()
        {
            _configure = Configure;
        }

        public ObjectTypeExtension(Action<IObjectTypeDescriptor> configure)
        {
            _configure = configure;
        }

        public override TypeKind Kind => TypeKind.Object;

        protected override ObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor =
                ObjectTypeDescriptor.New(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        internal override void Merge(
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is ObjectType objectType)
            {
                TypeExtensionHelper.MergeContextData(
                    Definition,
                    objectType.Definition);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition.Directives,
                    objectType.Definition.Directives);

                TypeExtensionHelper.MergeInterfaces(
                    Definition,
                    objectType.Definition);

                TypeExtensionHelper.MergeObjectFields(
                    context,
                    objectType.Definition.RuntimeType,
                    Definition.Fields,
                    objectType.Definition.Fields);

                TypeExtensionHelper.MergeConfigurations(
                    Definition.Configurations,
                    objectType.Definition.Configurations);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.ObjectTypeExtension_CannotMerge);
            }
        }
    }
}
