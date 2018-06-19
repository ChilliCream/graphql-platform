using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputObjectType
        : INamedInputType
        , INullableType
        , ITypeSystemNode
        , INeedsInitialization
    {
        private Dictionary<string, InputField> _fieldMap =
            new Dictionary<string, InputField>();
        private Type _nativeType;
        private Func<ObjectValueNode, object> _deserialize;

        internal InputObjectType()
        {
            Initialize(Configure);
        }

        internal InputObjectType(Action<IInputObjectTypeDescriptor> configure)
        {
            Initialize(configure);
        }

        public TypeKind Kind { get; } = TypeKind.InputObject;

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
                if (_deserialize == null)
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
            return InputObjectDefaultSerializer.ParseValue(this, value);
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
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            InputObjectTypeDescriptor descriptor = CreateDescriptor();
            configure(descriptor);
            Initialize(descriptor);
        }

        private void Initialize(InputObjectTypeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "An input object type name must not be null or empty.");
            }

            foreach (InputField field in
                descriptor.GetFieldDescriptors()
                .Select(t => new InputField(t)))
            {
                _fieldMap[field.Name] = field;
            }

            _nativeType = descriptor.NativeType;

            SyntaxNode = descriptor.SyntaxNode;
            Name = descriptor.Name;
            Description = descriptor.Description;
        }

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (InputField field in _fieldMap.Values)
            {
                field.RegisterDependencies(
                    schemaContext.Types, reportError, this);
            }
        }

        void INeedsInitialization.CompleteType(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            CompleteNativeType(schemaContext.Types, reportError);

            if (!_fieldMap.Any())
            {
                reportError(new SchemaError(
                    $"The input object `{Name}` does not have any fields."));
            }
        }

        private void CompleteNativeType(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError)
        {
            if (typeRegistry.TryGetTypeBinding(this,
                out InputObjectTypeBinding typeBinding))
            {
                _nativeType = typeBinding.Type;
                _deserialize = InputObjectDeserializerFactory.Create(
                    reportError, this, _nativeType);
            }
            else
            {
                reportError(new SchemaError(
                    "Could not resolve the native type associated with " +
                    $"input object type `{Name}`.",
                    this));
            }
        }

        #endregion
    }

    public class InputObjectType<T>
        : InputObjectType
    {
        public InputObjectType()
        {
        }

        public InputObjectType(Action<IInputObjectTypeDescriptor<T>> configure)
            : base(d => configure((IInputObjectTypeDescriptor<T>)d))
        {
        }

        #region Configuration

        internal sealed override InputObjectTypeDescriptor CreateDescriptor() =>
            new InputObjectTypeDescriptor<T>(typeof(T));

        protected sealed override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            Configure((IInputObjectTypeDescriptor<T>)descriptor);
        }

        protected virtual void Configure(IInputObjectTypeDescriptor<T> descriptor) { }

        #endregion
    }
}
