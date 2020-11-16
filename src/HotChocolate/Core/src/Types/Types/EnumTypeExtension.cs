using System;
using System.Globalization;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Properties;
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
            ITypeDiscoveryContext context)
        {
            var descriptor = EnumTypeDescriptor.New(
                context.DescriptorContext);
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            EnumTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        internal override void Merge(
            ITypeCompletionContext context,
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

                TypeExtensionHelper.MergeConfigurations(
                    Definition.Configurations,
                    enumType.Definition.Configurations);

                MergeValues(context, Definition, enumType.Definition);
            }
            else
            {
                throw new ArgumentException(
                    TypeResources.EnumTypeExtension_CannotMerge,
                    nameof(type));
            }
        }

        private void MergeValues(
            ITypeCompletionContext context,
            EnumTypeDefinition extension,
            EnumTypeDefinition type)
        {
            foreach (EnumValueDefinition enumValue in
                extension.Values.Where(t => t.Value != null))
            {
                if (type.RuntimeType.IsAssignableFrom(enumValue.Value.GetType()))
                {
                    EnumValueDefinition existingValue =
                        type.Values.FirstOrDefault(t =>
                            enumValue.Value.Equals(t.Value));

                    if (existingValue is null)
                    {
                        type.Values.Add(enumValue);
                    }
                    else
                    {
                        TypeExtensionHelper.MergeDirectives(
                            context,
                            enumValue.Directives,
                            existingValue.Directives);
                    }
                }
                else
                {
                    context.ReportError(
                        SchemaErrorBuilder.New()
                            .SetMessage(string.Format(
                                CultureInfo.InvariantCulture,
                                TypeResources.EnumTypeExtension_ValueTypeInvalid,
                                enumValue.Value))
                            .SetTypeSystemObject(this)
                            .Build());
                }
            }
        }
    }
}
