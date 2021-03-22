using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public partial class EnumType
    {
        private readonly Dictionary<NameString, IEnumValue> _enumValues = new();
        private readonly Dictionary<object, IEnumValue> _valueLookup = new();
        private Action<IEnumTypeDescriptor>? _configure;
        private INamingConventions _naming = default!;

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
            var descriptor =
                EnumTypeDescriptor.FromSchemaType( context.DescriptorContext, GetType());

            _configure!(descriptor);
            _configure = null;

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

            _naming = context.DescriptorContext.Naming;
            SyntaxNode = definition.SyntaxNode;

            foreach (EnumValueDefinition enumValueDefinition in definition.Values)
            {
                if (TryCreateEnumValue(context, enumValueDefinition, out IEnumValue? enumValue))
                {
                    _enumValues[enumValue.Name] = enumValue;
                    _valueLookup[enumValue.Value] = enumValue;
                }
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

        protected virtual bool TryCreateEnumValue(
            ITypeCompletionContext context,
            EnumValueDefinition definition,
            [NotNullWhen(true)]out IEnumValue? enumValue)
        {
            enumValue = new EnumValue(context, definition);
            return true;
        }
    }
}
