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
    public class InputObjectTypeExtension : NamedTypeExtensionBase<InputObjectTypeDefinition>
    {
        private Action<IInputObjectTypeDescriptor>? _configure;

        protected InputObjectTypeExtension()
        {
            _configure = Configure;
        }

        public InputObjectTypeExtension(
            Action<IInputObjectTypeDescriptor> configure)
        {
            _configure = configure;
        }

        public override TypeKind Kind { get; } = TypeKind.InputObject;

        protected override InputObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor =
                InputObjectTypeDescriptor.New(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInputObjectTypeDescriptor descriptor)
        {

        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        protected override void Merge(
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is InputObjectType inputObjectType)
            {
                // we first assert that extension and type are mutable and by 
                // this that they do have a type definition.
                AssertMutable();
                inputObjectType.AssertMutable();

                TypeExtensionHelper.MergeContextData(
                    Definition!,
                    inputObjectType.Definition!);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition!.Directives!,
                    inputObjectType.Definition!.Directives);

                TypeExtensionHelper.MergeInputObjectFields(
                    context,
                    Definition!.Fields,
                    inputObjectType.Definition!.Fields);

                TypeExtensionHelper.MergeConfigurations(
                    Definition!.Configurations,
                    inputObjectType.Definition!.Configurations);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.InputObjectTypeExtension_CannotMerge,
                    nameof(type));
            }
        }
    }
}
