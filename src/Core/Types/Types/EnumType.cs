using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class EnumType
        : NamedTypeBase
        , INamedOutputType
        , INamedInputType
        , ISerializableType
    {
        private readonly Dictionary<string, EnumValue> _nameToValues =
            new Dictionary<string, EnumValue>();
        private readonly Dictionary<object, EnumValue> _valueToValues =
            new Dictionary<object, EnumValue>();

        protected EnumType()
            : base(TypeKind.Enum)
        {
            Initialize(Configure);
        }

        public EnumType(Action<IEnumTypeDescriptor> configure)
            : base(TypeKind.Enum)
        {
            Initialize(configure);
        }

        public Type ClrType { get; private set; }

        public EnumTypeDefinitionNode SyntaxNode { get; private set; }

        public IReadOnlyCollection<EnumValue> Values => _nameToValues.Values;

        public bool TryGetValue(string name, out object value)
        {
            if (_nameToValues.TryGetValue(name, out EnumValue enumValue))
            {
                value = enumValue.Value;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetName(object value, out string name)
        {
            if (_valueToValues.TryGetValue(value, out EnumValue enumValue))
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
                TypeResources.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()),
                nameof(literal));
        }

        public IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return NullValueNode.Default;
            }

            if (_valueToValues.TryGetValue(value, out EnumValue enumValue))
            {
                return new EnumValueNode(enumValue.Name);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseValue(
                    Name, value.GetType()),
                nameof(value));
        }

        public object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (ClrType.IsInstanceOfType(value)
                && _valueToValues.TryGetValue(value, out EnumValue enumValue))
            {
                return enumValue.Name;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        public object Deserialize(object value)
        {
            if (TryDeserialize(value, out object v))
            {
                return v;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Deserialize(Name));
        }

        public bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string name
                && _nameToValues.TryGetValue(name, out EnumValue enumValue))
            {
                value = enumValue.Value;
                return true;
            }

            value = null;
            return false;
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

            EnumTypeDescription description = descriptor.CreateDescription();

            foreach (EnumValue enumValue in description.Values
                .Select(t => new EnumValue(t)))
            {
                _nameToValues[enumValue.Name] = enumValue;
                _valueToValues[enumValue.Value] = enumValue;
            }

            SyntaxNode = description.SyntaxNode;
            ClrType = description.ClrType;

            Initialize(description.Name, description.Description,
                new DirectiveCollection(
                    this,
                    DirectiveLocation.Enum,
                    description.Directives));
        }

        protected override void OnCompleteType(
            ITypeInitializationContext context)
        {
            if (!Values.Any())
            {
                // TODO : Resources
                context.ReportError(new SchemaError(
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
            new EnumTypeDescriptor<T>();

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
