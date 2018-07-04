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
        , INeedsInitialization
    {
        private Func<ObjectValueNode, object> _deserialize;

        internal InputObjectType(Action<IInputObjectTypeDescriptor> configure)
            : this(ExecuteConfigure(configure))
        {
        }

        internal InputObjectType(Func<InputObjectTypeDescription> descriptionFactory)
            : this(DescriptorHelpers.ExecuteFactory(descriptionFactory))
        {
        }

        internal InputObjectType(
            InputObjectTypeDescription inputObjectTypeDescription)
        {
            if (inputObjectTypeDescription == null)
            {
                throw new ArgumentNullException(nameof(inputObjectTypeDescription));
            }

            if (string.IsNullOrEmpty(inputObjectTypeDescription.Name))
            {
                throw new ArgumentException(
                    "An input object type name must not be null or empty.");
            }

            NativeType = inputObjectTypeDescription.NativeType;
            SyntaxNode = inputObjectTypeDescription.SyntaxNode;
            Name = inputObjectTypeDescription.Name;
            Description = inputObjectTypeDescription.Description;
            Fields = new FieldCollection<InputField>(
                inputObjectTypeDescription.Fields.Select(t => new InputField(t)));
        }

        private static InputObjectTypeDescription ExecuteConfigure(
            Action<IInputObjectTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            InputObjectTypeDescriptor descriptor = new InputObjectTypeDescriptor();
            configure(descriptor);
            return descriptor.CreateDescription();
        }

        private static InterfaceFieldDescription ExecuteConfigure(
            Action<IInterfaceFieldDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            InterfaceFieldDescriptor descriptor = new InterfaceFieldDescriptor();
            configure(descriptor);
            return descriptor.CreateDescription();
        }

        public TypeKind Kind { get; } = TypeKind.InputObject;

        public InputObjectTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public FieldCollection<InputField> Fields { get; }

        public Type NativeType { get; private set; }

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

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The input object type can only parse object value literals.",
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

        }

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            foreach (InputField field in Fields)
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
            CompleteFields(schemaContext.Types, reportError);
        }

        private void CompleteNativeType(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError)
        {
            if (NativeType == null && typeRegistry.TryGetTypeBinding(this,
                out InputObjectTypeBinding typeBinding))
            {
                NativeType = typeBinding.Type;
            }

            if (NativeType == null)
            {
                reportError(new SchemaError(
                    "Could not resolve the native type associated with " +
                    $"input object type `{Name}`.",
                    this));
            }

            _deserialize = InputObjectDeserializerFactory.Create(
                    reportError, this, NativeType);
        }

        private void CompleteFields(
            ITypeRegistry typeRegistry,
            Action<SchemaError> reportError)
        {
            foreach (InputField field in Fields)
            {
                field.CompleteInputField(typeRegistry, reportError, this);
            }

            if (Fields.IsEmpty)
            {
                reportError(new SchemaError(
                    $"The input object `{Name}` does not have any fields."));
            }
        }

        #endregion
    }

    public class InputObjectType<T>
        : InputObjectType
    {
        public InputObjectType()
            : this(d => { })
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
