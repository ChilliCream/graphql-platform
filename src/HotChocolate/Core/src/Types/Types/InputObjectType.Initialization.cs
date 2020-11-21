using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Serialization;
using static HotChocolate.Utilities.Serialization.InputObjectParserHelper;
using static HotChocolate.Utilities.Serialization.InputObjectConstructorResolver;
using static HotChocolate.Utilities.Serialization.InputObjectCompiler;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL input object type
    /// </summary>
    public partial class InputObjectType
    {
        private Action<IInputObjectTypeDescriptor>? _configure;
        private InputObjectToObjectValueConverter _objectToValueConverter = default!;
        private InputObjectToDictionaryConverter _objectToDictionary = default!;
        private Func<ObjectValueNode, object> _parseLiteral = default!;
        private Func<IReadOnlyDictionary<string, object>, object> _deserialize = default!;

        protected InputObjectType()
        {
            _configure = Configure;
        }

        public InputObjectType(Action<IInputObjectTypeDescriptor> configure)
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
        protected virtual void Configure(IInputObjectTypeDescriptor descriptor)
        {
        }

        protected override InputObjectTypeDefinition CreateDefinition(
            ITypeDiscoveryContext context)
        {
            var descriptor = InputObjectTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure!(descriptor);
            _configure = null;
            return descriptor.CreateDefinition();
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
            SetTypeIdentity(typeof(InputObjectType<>));
        }

        protected override void OnCompleteType(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            ITypeConverter converter = context.Services.GetTypeConverter();

            _objectToValueConverter = new InputObjectToObjectValueConverter(converter);
            _objectToDictionary = new InputObjectToDictionaryConverter(converter);

            SyntaxNode = definition.SyntaxNode;

            var fields = new List<InputField>();
            OnCompleteFields(context, definition, fields);

            Fields = new FieldCollection<InputField>(
                fields,
                context.DescriptorContext.Options.SortFieldsByName);
            FieldInitHelper.CompleteFields(context, definition, Fields);

            if (RuntimeType == typeof(object) || Fields.Any(t => t.Property is null))
            {
                _parseLiteral = ov => ParseLiteralToDictionary(this, ov, converter);
                _deserialize = map => DeserializeToDictionary(this, map, converter);
            }
            else
            {
                IEnumerable<PropertyInfo> properties = Fields.Select(t => t.Property!);
                ConstructorInfo? constructor = GetConstructor(RuntimeType, properties);
                InputObjectFactory factory = CompileFactory(this, constructor);

                _parseLiteral =
                    ov => InputObjectParserHelper.ParseLiteral(this, ov, factory, converter);
                _deserialize =
                    map => InputObjectParserHelper.Deserialize(this, map, factory, converter);
            }
        }

        protected virtual void OnCompleteFields(
            ITypeCompletionContext context,
            InputObjectTypeDefinition definition,
            ICollection<InputField> fields)
        {
            foreach (InputFieldDefinition fieldDefinition in definition.Fields)
            {
                fields.Add(new InputField(fieldDefinition));
            }
        }
    }
}
