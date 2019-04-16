using System.Collections.Generic;
using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using System.Linq;

namespace HotChocolate.Types
{
    public class InterfaceTypeExtension
        : NamedTypeExtensionBase<InterfaceTypeDefinition>
    {
        private readonly Action<IInterfaceTypeDescriptor> _configure;

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
            IInitializationContext context)
        {
            var descriptor = InterfaceTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IInterfaceTypeDescriptor descriptor)
        { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            InterfaceTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        internal override void Merge(
            ICompletionContext context,
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
            }
            else
            {
                // TODO : resources
                throw new ArgumentException("CANNOT MERGE");
            }
        }
    }
}
