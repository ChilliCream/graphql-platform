using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class InputObjectTypeExtension
        : NamedTypeExtensionBase<InputObjectTypeDefinition>
    {
        private readonly Action<IInputObjectTypeDescriptor> _configure;

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
            IInitializationContext context)
        {
            var descriptor =
                InputObjectTypeDescriptor.New(
                    DescriptorContext.Create(context.Services),
                    GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInputObjectTypeDescriptor descriptor)
        {

        }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        internal override void Merge(
            ICompletionContext context,
            INamedType type)
        {
            if (type is InputObjectType inputObjectType)
            {
                TypeExtensionHelper.MergeContextData(
                    Definition,
                    inputObjectType.Definition);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition.Directives,
                    inputObjectType.Definition.Directives);

                TypeExtensionHelper.MergeInputObjectFields(
                    context,
                    Definition.Fields,
                    inputObjectType.Definition.Fields);
            }
            else
            {
                // TODO : resources
                throw new ArgumentException("CANNOT MERGE");
            }
        }
    }
}
