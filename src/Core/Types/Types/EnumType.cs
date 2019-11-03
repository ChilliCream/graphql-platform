using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public class EnumType
        : NamedTypeBase<EnumTypeDefinition>
        , ILeafType
    {
        private readonly Action<IEnumTypeDescriptor> _configure;
        private readonly Dictionary<string, EnumValue> _nameToValues =
            new Dictionary<string, EnumValue>();
        private readonly Dictionary<object, EnumValue> _valueToValues =
            new Dictionary<object, EnumValue>();

        protected EnumType()
        {
            _configure = Configure;
        }

        public EnumType(Action<IEnumTypeDescriptor> configure)
        {
            _configure = configure
                ?? throw new ArgumentNullException(nameof(configure));
        }

        public override TypeKind Kind => TypeKind.Enum;

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
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()),
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
            if (value == null)
            {
                return NullValueNode.Default;
            }

            if (_valueToValues.TryGetValue(value, out EnumValue enumValue))
            {
                return new EnumValueNode(enumValue.Name);
            }

            throw new ArgumentException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
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

            // schema first unbound enum type
            if (ClrType == typeof(object)
                && _nameToValues.TryGetValue(
                    value.ToString().ToUpperInvariant(),
                    out enumValue))
            {
                return enumValue.Name;
            }

            throw new ArgumentException(
                TypeResourceHelper.Scalar_Cannot_Serialize(Name));
        }

        public object Deserialize(object serialized)
        {
            if (TryDeserialize(serialized, out object v))
            {
                return v;
            }

            throw new ArgumentException(
                TypeResourceHelper.Scalar_Cannot_Deserialize(Name));
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

            if (_valueToValues.TryGetValue(serialized, out enumValue))
            {
                value = enumValue.Value;
                return true;
            }

            value = null;
            return false;
        }

        #endregion

        #region Initialization

        protected override EnumTypeDefinition CreateDefinition(
            IInitializationContext context)
        {
            var descriptor = EnumTypeDescriptor.FromSchemaType(
                context.DescriptorContext,
                GetType());
            _configure(descriptor);
            return descriptor.CreateDefinition();
        }

        protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

        protected override void OnRegisterDependencies(
            IInitializationContext context,
            EnumTypeDefinition definition)
        {
            base.OnRegisterDependencies(context, definition);
            context.RegisterDependencies(definition);
        }

        protected override void OnCompleteType(
            ICompletionContext context,
            EnumTypeDefinition definition)
        {
            base.OnCompleteType(context, definition);

            SyntaxNode = definition.SyntaxNode;

            foreach (EnumValue enumValue in definition.Values
                .Select(t => new EnumValue(t)))
            {
                _nameToValues[enumValue.Name] = enumValue;
                _valueToValues[enumValue.Value] = enumValue;
                enumValue.CompleteValue(context);
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

        #endregion
    }
}
