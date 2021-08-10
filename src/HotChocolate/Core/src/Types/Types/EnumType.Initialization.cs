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
    /// <summary>
    /// GraphQL Enum types, like Scalar types, also represent leaf values in a GraphQL type system.
    /// However Enum types describe the set of possible values.
    ///
    /// Enums are not references for a numeric value, but are unique values in their own right.
    /// They may serialize as a string: the name of the represented value.
    ///
    /// In this example, an Enum type called Direction is defined:
    /// </summary>
    public partial class EnumType
    {
        private readonly Dictionary<NameString, IEnumValue> _enumValues = new();
        private readonly Dictionary<object, IEnumValue> _valueLookup = new();
        private Action<IEnumTypeDescriptor>? _configure;
        private INamingConventions _naming = default!;

        /// <summary>
        /// Initializes a new instance of <see cref="EnumType"/>.
        /// </summary>
        protected EnumType()
        {
            _configure = Configure;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="EnumType"/>.
        /// </summary>
        /// <param name="configure">
        /// A delegate defining the configuration.
        /// </param>
        public EnumType(Action<IEnumTypeDescriptor> configure)
        {
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        /// <summary>
        /// Create an enum type from a type definition.
        /// </summary>
        /// <param name="definition">
        /// The enum type definition that specifies the properties of the newly created enum type.
        /// </param>
        /// <returns>
        /// Returns the newly created enum type.
        /// </returns>
        public static EnumType CreateUnsafe(EnumTypeDefinition definition)
            => new() { Definition = definition};

        /// <summary>
        /// Override this in order to specify the type configuration explicitly.
        /// </summary>
        /// <param name="descriptor">
        /// The descriptor of this type lets you express the type configuration.
        /// </param>
        protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

        /// <inheritdoc />
        protected override EnumTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        {
            try
            {
                if (Definition is null)
                {
                    var descriptor = EnumTypeDescriptor.FromSchemaType(
                        context.DescriptorContext,
                        GetType());
                    _configure!(descriptor);
                    return descriptor.CreateDefinition();
                }

                return Definition;
            }
            finally
            {
                _configure = null;
            }
        }

        /// <inheritdoc />
        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            EnumTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(EnumType<>));
        }

        /// <inheritdoc />
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
