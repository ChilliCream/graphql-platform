using System;
using System.Globalization;
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
    public class EnumTypeExtension : NamedTypeExtensionBase<EnumTypeDefinition>
    {
        private Action<IEnumTypeDescriptor>? _configure;

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
            var descriptor =
                EnumTypeDescriptor.New(context.DescriptorContext);

            _configure!(descriptor);
            _configure = null;

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

        protected override void Merge(
            ITypeCompletionContext context,
            INamedType type)
        {
            if (type is EnumType enumType)
            {
                // we first assert that extension and type are mutable and by 
                // this that they do have a type definition.
                AssertMutable();
                enumType.AssertMutable();

                TypeExtensionHelper.MergeContextData(
                    Definition!,
                    enumType.Definition!);

                TypeExtensionHelper.MergeDirectives(
                    context,
                    Definition!.Directives,
                    enumType.Definition!.Directives);

                TypeExtensionHelper.MergeConfigurations(
                    Definition!.Configurations,
                    enumType.Definition!.Configurations);

                MergeValues(context, Definition!, enumType.Definition!);
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
                if (type.RuntimeType.IsInstanceOfType(enumValue.Value))
                {
                    EnumValueDefinition? existingValue =
                        type.Values.FirstOrDefault(t =>
                            enumValue.Value.Equals(t.Value));

                    if (existingValue is null)
                    {
                        type.Values.Add(enumValue);
                    }
                    else
                    {
                        TypeExtensionHelper.MergeContextData(enumValue, existingValue);

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
