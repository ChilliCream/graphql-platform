using System;
using HotChocolate.Configuration;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class ObjectTypeExtension
        : NamedTypeExtensionBase<ObjectTypeDefinition>
    {
        private readonly Action<IObjectTypeDescriptor> _configure;

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
            IInitializationContext context)
        {
            var descriptor = ObjectTypeDescriptor.New(
                context.DescriptorContext);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IObjectTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        internal override void Merge(
            ICompletionContext context,
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

                TypeExtensionHelper.MergeObjectFields(
                    context,
                    objectType.Definition.ClrType,
                    Definition.Fields,
                    objectType.Definition.Fields);

                TypeExtensionHelper.MergeInterfaces(
                    Definition,
                    objectType.Definition);

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
