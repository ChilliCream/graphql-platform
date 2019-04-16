using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class EnumTypeExtension
        : NamedTypeExtensionBase<EnumTypeDefinition>
    {
        private readonly Action<IEnumTypeDescriptor> _configure;

        protected EnumTypeExtension()
        {
            _configure = Configure;
        }

        public EnumTypeExtension(Action<IEnumTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.Enum;

        protected override EnumTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = EnumTypeDescriptor.New(
                DescriptorContext.Create(context.Services),
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            EnumTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        internal override void Merge(
            ICompletionContext context,
            INamedType type)
        {
            if (type is EnumType enumType)
            {
                TypeExtensionHelper.MergeContextData(
                    Definition,
                    enumType.Definition);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition.Directives,
                    enumType.Definition.Directives);

                MergeValues(context, Definition, enumType.Definition);
            }
            else
            {
                // TODO : resources
                throw new ArgumentException("CANNOT MERGE");
            }
        }

        private void MergeValues(
            ICompletionContext context,
            EnumTypeDefinition extension,
            EnumTypeDefinition type)
        {
            // TODO : we have to rework this once directive support is in.
            foreach (EnumValueDefinition enumValue in
                extension.Values.Where(t => t.Value != null))
            {
                if (type.ClrType.IsAssignableFrom(enumValue.Value.GetType()))
                {
                    type.Values.Add(enumValue);
                }
                else
                {
                    // TODO : resources
                    context.ReportError(
                        SchemaErrorBuilder.New()
                            .SetMessage("Invalid Type")
                            .SetTypeSystemObject(this)
                            .Build());
                }
            }
        }
    }
}
