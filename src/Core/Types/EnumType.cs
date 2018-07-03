using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class EnumType
        : INamedOutputType
        , INamedInputType
        , ISerializableType
        , INeedsInitialization
    {
        private readonly Dictionary<string, EnumValue> _nameToValues =
            new Dictionary<string, EnumValue>();
        private readonly Dictionary<object, EnumValue> _valueToValues =
            new Dictionary<object, EnumValue>();

        protected EnumType()
        {
            Initialize(Configure);
        }

        public EnumType(Action<IEnumTypeDescriptor> configure)
        {
            Initialize(configure);
        }

        public TypeKind Kind { get; } = TypeKind.Enum;

        public EnumTypeDefinitionNode SyntaxNode { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyCollection<EnumValue> Values => _nameToValues.Values;

        public Type NativeType { get; private set; }

        public bool TryGetValue(string name, out object value)
        {
            if (_nameToValues.TryGetValue(name, out var enumValue))
            {
                value = enumValue.Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetName(object value, out string name)
        {
            if (_valueToValues.TryGetValue(value, out var enumValue))
            {
                name = enumValue.Name;
                return true;
            }

            name = null;
            return false;
        }

        #region Serialization

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is EnumValueNode ev)
            {
                return _nameToValues.ContainsKey(ev.Value);
            }
            return false;
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is EnumValueNode evn
                && _nameToValues.TryGetValue(evn.Value, out EnumValue ev))
            {
                return ev.Value;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The specified value cannot be handled " +
                $"by the EnumType {Name}.");
        }

        public IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode();
            }

            if (_valueToValues.TryGetValue(value, out EnumValue enumValue))
            {
                return new EnumValueNode(enumValue.Name);
            }

            throw new ArgumentException(
                "The specified value has to be a defined enum value of the type " +
                $"{NativeType.FullName} to be parsed by this enum type.");
        }

        public object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (NativeType.IsInstanceOfType(value)
                && _valueToValues.TryGetValue(value, out EnumValue enumValue))
            {
                return enumValue.Name;
            }

            throw new ArgumentException(
                $"The specified value cannot be handled by the EnumType `{Name}`.");
        }

        #endregion

        #region Configuration

        internal virtual EnumTypeDescriptor CreateDescriptor() =>
            new EnumTypeDescriptor(GetType().Name);

        protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

        #endregion

        #region  Initialization

        private void Initialize(Action<IEnumTypeDescriptor> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            EnumTypeDescriptor descriptor = CreateDescriptor();
            configure(descriptor);
            Initialize(descriptor);
        }

        private void Initialize(EnumTypeDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "Am enum type name must not be null or empty.");
            }

            foreach (EnumValue enumValue in descriptor.GetItems()
                .Select(t => new EnumValue(t)))
            {
                _nameToValues[enumValue.Name] = enumValue;
                _valueToValues[enumValue.Value] = enumValue;
            }

            SyntaxNode = descriptor.SyntaxNode;
            Name = descriptor.Name;
            Description = descriptor.Description;
            NativeType = descriptor.NativeType;
        }

        void INeedsInitialization.RegisterDependencies(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
        }

        void INeedsInitialization.CompleteType(
            ISchemaContext schemaContext,
            Action<SchemaError> reportError)
        {
            if (!Values.Any())
            {
                reportError(new SchemaError(
                    $"The enum type `{Name}` has no values."));
            }
        }

        #endregion
    }

    public class EnumType<T>
        : EnumType
    {
        public EnumType()
        {
        }

        public EnumType(Action<IEnumTypeDescriptor<T>> configure)
            : base(d => configure((IEnumTypeDescriptor<T>)d))
        {
        }

        #region Configuration

        internal sealed override EnumTypeDescriptor CreateDescriptor() =>
            new EnumTypeDescriptor<T>(typeof(T));

        protected sealed override void Configure(IEnumTypeDescriptor descriptor)
        {
            Configure((IEnumTypeDescriptor<T>)descriptor);
        }

        protected virtual void Configure(IEnumTypeDescriptor<T> descriptor)
        {
        }

        #endregion
    }
}
