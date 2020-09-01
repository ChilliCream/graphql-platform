using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public partial class EnumType
    {
        private readonly Action<IEnumTypeDescriptor> _configure;
        private readonly Dictionary<string, EnumValue> _nameToValues =
            new Dictionary<string, EnumValue>();
        private readonly Dictionary<object, EnumValue> _valueToValues =
            new Dictionary<object, EnumValue>();

        protected EnumType()
        {
            _configure = Configure;
        }

        public EnumType(Action<IEnumTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        /// <summary>
        /// Override this in order to specify the type configuration explicitly.
        /// </summary>
        /// <param name="descriptor">
        /// The descriptor of this type lets you express the type configuration.
        /// </param>
        protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

        protected override EnumTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = EnumTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }


        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            EnumTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(EnumType<>));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            EnumTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            SyntaxNode = definition.SyntaxNode;

            foreach (EnumValue enumValue in definition.Values
                .Select(t => new EnumValue(t)))
            {
                _nameToValues[enumValue.Name] = enumValue;
                _valueToValues[enumValue.Value] = enumValue;
                enumValue.CompleteValue(context);
            }

            if (!Values.Any())
            {
                context.ReportError(
                    SchemaErrorBuilder.New()
                        .SetMessage(TypeResources.EnumType_NoValues, Name)
                        .SetCode(ErrorCodes.Schema.NoEnumValues)
                        .SetTypeSystemObject(this)
                        .AddSyntaxNode(SyntaxNode)
                        .Build());
            }
        }
    }
}
