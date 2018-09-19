using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class InputObjectType
        : TypeBase
        , INamedInputType
    {
        private Func<ObjectValueNode, object> _deserialize;

        internal InputObjectType()
            : base(TypeKind.InputObject)
        {
            Initialize(Configure);
        }

        internal InputObjectType(Action<IInputObjectTypeDescriptor> configure)
            : base(TypeKind.InputObject)
        {
            Initialize(configure);
        }

        public InputObjectTypeDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public FieldCollection<InputField> Fields { get; private set; }

        public Type ClrType { get; private set; }

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

            InputObjectTypeDescription description = descriptor.CreateDescription();
            ClrType = description.NativeType;
            SyntaxNode = description.SyntaxNode;
            Name = description.Name;
            Description = description.Description;
            Fields = new FieldCollection<InputField>(
                description.Fields.Select(t => new InputField(t)));
        }

        protected override void OnRegisterDependencies(
            ITypeInitializationContext context)
        {
            base.OnRegisterDependencies(context);

            foreach (INeedsInitialization field in Fields
                .Cast<INeedsInitialization>())
            {
                field.RegisterDependencies(context);
            }
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            base.OnCompleteType(context);

            CompleteNativeType(context);
            CompleteFields(context);
        }

        private void CompleteNativeType(
            ITypeInitializationContext context)
        {
            if (ClrType == null
                && context.TryGetNativeType(this, out Type nativeType))
            {
                ClrType = nativeType;
            }

            if (ClrType == null)
            {
                context.ReportError(new SchemaError(
                    "Could not resolve the native type associated with " +
                    $"input object type `{Name}`.",
                    this));
            }

            _deserialize = InputObjectDeserializerFactory.Create(
                    context, this, ClrType);
        }

        private void CompleteFields(
            ITypeInitializationContext context)
        {
            foreach (INeedsInitialization field in Fields
                .Cast<INeedsInitialization>())
            {
                field.CompleteType(context);
            }

            if (Fields.IsEmpty)
            {
                context.ReportError(new SchemaError(
                    $"The input object `{Name}` does not have any fields."));
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
