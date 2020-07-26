using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Serialization;

namespace HotChocolate.Types
{
    public class InputObjectType
        : NamedTypeBase<InputObjectTypeDefinition>
        , INamedInputType
    {
        private readonly Action<IInputObjectTypeDescriptor> _configure;
        private InputObjectToObjectValueConverter _objectToValueConverter;
        private InputObjectToDictionaryConverter _objectToDictionary;
        private Func<ObjectValueNode, object> _parseLiteral;
        private Func<IReadOnlyDictionary<string, object>, object> _deserialize;

        protected InputObjectType()
        {
            _configure = Configure;
        }

        public InputObjectType(
            Action<IInputObjectTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.InputObject;

        public InputObjectTypeDefinitionNode SyntaxNode { get; private set; }

        public FieldCollection<InputField> Fields { get; private set; }

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is ObjectValueNode
                || literal is NullValueNode;
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is ObjectValueNode ov)
            {
                return _parseLiteral(ov);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new InputObjectSerializationException(
                TypeResources.InputObjectType_CannotParseLiteral);
        }

        public bool IsInstanceOfType(object value)
        {
            if (value is null)
            {
                return true;
            }

            return ClrType.IsInstanceOfType(value);
        }

        public IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            return _objectToValueConverter.Convert(this, value);
        }

        public object Serialize(object value)
        {
            if (TrySerialize(value, out object serialized))
            {
                return serialized;
            }
            throw new InputObjectSerializationException(
                "The specified value is not a valid input object.");
        }

        public virtual bool TrySerialize(object value, out object serialized)
        {
            try
            {
                if (value is null)
                {
                    serialized = null;
                    return true;
                }

                if (value is IReadOnlyDictionary<string, object>
                    || value is IDictionary<string, object>)
                {
                    serialized = value;
                    return true;
                }

                serialized = _objectToDictionary.Convert(this, value);
                return true;
            }
            catch
            {
                serialized = null;
                return false;
            }
        }

        public object Deserialize(object serialized)
        {
            if (serialized is null)
            {
                return null;
            }

            if (serialized is IReadOnlyDictionary<string, object> dict)
            {
                return _deserialize(dict);
            }

            if (ClrType != typeof(object) && ClrType.IsInstanceOfType(serialized))
            {
                return serialized;
            }

            throw new InputObjectSerializationException(
                "The specified value is not a serialized input object.");
        }

        public virtual bool TryDeserialize(object serialized, out object value)
        {
            try
            {
                if (serialized is null)
                {
                    value = null;
                    return true;
                }

                if (serialized is IReadOnlyDictionary<string, object> dict)
                {
                    value = _deserialize(dict);
                    return true;
                }

                if (ClrType != typeof(object) && ClrType.IsInstanceOfType(serialized))
                {
                    value = serialized;
                    return true;
                }


                value = null;
                return false;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        protected override InputObjectTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = InputObjectTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
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
            SetTypeIdentity(typeof(InputObjectType<>));
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            ITypeConversion converter = context.Services.GetTypeConversion();

            _objectToValueConverter =
                new InputObjectToObjectValueConverter(converter);
            _objectToDictionary =
                new InputObjectToDictionaryConverter(converter);

            SyntaxNode = definition.SyntaxNode;

            var fields = new List<InputField>();
            OnCompleteFields(context, definition, fields);

            Fields = new FieldCollection<InputField>(fields);
            FieldInitHelper.CompleteFields(context, definition, Fields);

            if (ClrType == typeof(object) || Fields.Any(t => t.Property is null))
            {
                _parseLiteral = ov => InputObjectParserHelper.Parse(this, ov, converter);
                _deserialize = map => InputObjectParserHelper.Deserialize(this, map, converter);
            }
            else
            {
                ConstructorInfo constructor = InputObjectConstructorResolver.GetConstructor(
                    this.ClrType,
                    Fields.Select(t => t.Property));
                InputObjectFactory factory = InputObjectFactoryCompiler.Compile(this, constructor);

                _parseLiteral = ov => InputObjectParserHelper.Parse(
                    this, ov, factory, converter);

                _deserialize = map => InputObjectParserHelper.Deserialize(
                    this, map, factory, converter);
            }
        }

        protected virtual void OnCompleteFields(
            ICompletionContext context,
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
