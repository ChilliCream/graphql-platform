using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    public class InputObjectType
        : NamedTypeBase<InputObjectTypeDefinition>
        , INamedInputType
    {
        private readonly Action<IInputObjectTypeDescriptor> _configure;
        private InputObjectToObjectValueConverter _objectToValueConverter;
        private ObjectValueToInputObjectConverter _valueToObjectConverter;
        private InputObjectToDictionaryConverter _objectToDictionary;
        private DictionaryToInputObjectConverter _dictionaryToObject;

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

        #region IInputType

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
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is ObjectValueNode ov)
            {
                return _valueToObjectConverter.Convert(ov, this);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                TypeResources.InputObjectType_CannotParseLiteral,
                nameof(literal));
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
            if (value is null)
            {
                return null;
            }

            if (value is IReadOnlyDictionary<string, object>
                || value is IDictionary<string, object>)
            {
                return value;
            }

            return _objectToDictionary.Convert(this, value);
        }

        public virtual object Deserialize(object serialized)
        {
            if (serialized is null)
            {
                return null;
            }

            if ((serialized is IReadOnlyDictionary<string, object>
                || serialized is IDictionary<string, object>)
                && ClrType == typeof(object))
            {
                return serialized;
            }

            return _dictionaryToObject.Convert(serialized, this);
        }

        public bool TryDeserialize(object serialized, out object value)
        {
            try
            {
                value = Deserialize(serialized);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        #endregion

        #region Initialization

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
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            InputObjectTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            ITypeConversion typeConversion =
                context.Services.GetTypeConversion();
            _objectToValueConverter =
                new InputObjectToObjectValueConverter(typeConversion);
            _valueToObjectConverter =
                new ObjectValueToInputObjectConverter(typeConversion);
            _objectToDictionary =
                new InputObjectToDictionaryConverter(typeConversion);
            _dictionaryToObject =
                new DictionaryToInputObjectConverter(typeConversion);

            SyntaxNode = definition.SyntaxNode;

            var fields = new List<InputField>();
            OnCompleteFields(context, definition, fields);

            Fields = new FieldCollection<InputField>(fields);
            FieldInitHelper.CompleteFields(context, definition, Fields);
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

        #endregion
    }
}
