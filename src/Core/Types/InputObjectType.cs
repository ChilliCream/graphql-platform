using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputObjectType
        : INamedType
        , IInputType
        , INullableType
        , ITypeSystemNode
        , INeedsInitialization
    {
        private Dictionary<string, InputField> _fieldMap =
            new Dictionary<string, InputField>();
        private Func<ITypeRegistry, Type> _nativeTypeFactory;
        private Type _nativeType;
        private Func<ObjectValueNode, object> _deserialize;
        private bool _hasDeserializer;

        internal InputObjectType(Action<IInputObjectTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            Initialize(configure);
        }

        internal InputObjectType()
        {
            Initialize(Configure);
        }

        internal InputObjectType(InputObjectTypeConfig config)
        {
            Initialize(config);
        }

        public InputObjectTypeDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyDictionary<string, InputField> Fields => _fieldMap;

        public Type NativeType => _nativeType;

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

            if (_deserialize == null)
            {
                throw new InvalidOperationException(
                    "No deserializer was configured for " +
                    $"input object type `{Name}`.");
            }

            if (literal is ObjectValueNode ov)
            {
                return _deserialize(ov);
            }

            throw new ArgumentException(
                "The input object type can only parse object value literals.",
                nameof(literal));
        }

        private object ParseLiteralWithParser(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is ObjectValueNode objectLiteral)
            {
                if (!_hasDeserializer)
                {
                    throw new InvalidOperationException(
                        "There is no deserializer availabel for input " +
                        $"object type `{Name}`");
                }
                return _deserialize(objectLiteral);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The string type can only parse string literals.",
                nameof(literal));
        }

        public IValueNode ParseValue(object value)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Configuration

        internal virtual InputObjectTypeDescriptor CreateDescriptor() =>
            new InputObjectTypeDescriptor();

        protected virtual void Configure(IInputObjectTypeDescriptor descriptor) { }

        #endregion

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;

        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes()
        {
            return Fields.Values;
        }

        #endregion

        #region Initialization

        private void Initialize(Action<IInputObjectTypeDescriptor> configure)
        {
            InputObjectTypeDescriptor descriptor = CreateDescriptor();
            configure(descriptor);

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "An input object type name must not be null or empty.");
            }

            if (!descriptor.Fields.Any())
            {
                throw new ArgumentException(
                    $"The input object `{descriptor.Name}` must at least " +
                    "provide one field.");
            }

            foreach (InputFieldDescriptor fieldDescriptor in descriptor.Fields)
            {
                _fieldMap[fieldDescriptor.Name] = fieldDescriptor.CreateField();
            }

            _nativeType = descriptor.NativeType;

            Name = descriptor.Name;
            Description = descriptor.Description;
        }

        private void Initialize(InputObjectTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "An input object type name must not be null or empty.",
                    nameof(config));
            }

            InputField[] fields = config.Fields?.ToArray();
            if (fields?.Length == 0)
            {
                throw new ArgumentException(
                   $"The input object `{config.Name}` must at least " +
                   "provide one field.",
                   nameof(config));
            }

            foreach (InputField field in fields)
            {
                _fieldMap[field.Name] = field;
            }

            _nativeTypeFactory = config.NativeType;

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        void INeedsInitialization.RegisterDependencies(ISchemaContext schemaContext, Action<SchemaError> reportError)
        {
            foreach (InputField field in _fieldMap.Values)
            {
                field.RegisterDependencies(schemaContext.Types, reportError, this);
            }
        }

        void INeedsInitialization.CompleteType(ISchemaContext schemaContext, Action<SchemaError> reportError)
        {
            _nativeType = _nativeTypeFactory(schemaContext.Types);
            if (_nativeType == null)
            {
                reportError(new SchemaError(
                    "Could not resolve the native type associated with " +
                    $"input object type `{Name}`.",
                    this));
            }
            else
            {
                _deserialize = InputObjectDeserializerFactory.Create(
                    reportError, this, _nativeType);
            }

            foreach (InputField field in _fieldMap.Values)
            {
                field.CompleteInputField(schemaContext.Types, reportError, this);
            }
        }

        #endregion
    }

    public class InputObjectType<T>
        : InputObjectType
    {
        public InputObjectType(Action<IInputObjectTypeDescriptor<T>> configure)
            : base(d => configure((IInputObjectTypeDescriptor<T>)d))
        {
        }

        protected InputObjectType()
        {
        }

        #region Configuration

        internal sealed override InputObjectTypeDescriptor CreateDescriptor() =>
            new InputObjectTypeDescriptor<T>();

        protected sealed override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            Configure((IInputObjectTypeDescriptor<T>)descriptor);
        }

        protected void Configure(IInputObjectTypeDescriptor<T> descriptor) { }

        #endregion
    }
}
