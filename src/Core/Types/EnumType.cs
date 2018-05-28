using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class EnumType
        : INamedType
        , IOutputType
        , IInputType
        , INullableType
        , ISerializableType
        , ITypeSystemNode
    {
        private readonly Dictionary<string, EnumValue> _nameToValues =
            new Dictionary<string, EnumValue>();
        private readonly Dictionary<object, EnumValue> _valueToValues =
            new Dictionary<object, EnumValue>();

        public EnumType()
        {
            EnumTypeDescriptor descriptor = new EnumTypeDescriptor(GetType());
            Configure(descriptor);

            if (string.IsNullOrEmpty(descriptor.Name))
            {
                throw new ArgumentException(
                    "Am enum type name must not be null or empty.");
            }

            if (descriptor.Items == null)
            {
                throw new ArgumentException(
                    $"The enum type {descriptor.Name} has no values.");
            }

            foreach (EnumValueConfig enumValueConfig in config.Values)
            {
                if (NativeType == null && enumValueConfig.Value != null)
                {
                    // TODO : what to do if:
                    // - values are not of the same type
                    // - one or more values are null
                    NativeType = enumValueConfig.Value.GetType();
                }

                EnumValue enumValue = new EnumValue(enumValueConfig);
                _nameToValues[enumValueConfig.Name] = enumValue;
                _valueToValues[enumValueConfig.Value] = enumValue;
            }

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }

        internal EnumType(EnumTypeConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrEmpty(config.Name))
            {
                throw new ArgumentException(
                    "Am enum type name must not be null or empty.",
                    nameof(config));
            }

            if (config.Values == null)
            {
                throw new ArgumentException(
                    $"The enum type {config.Name} has no values.",
                    nameof(config));
            }

            foreach (EnumValueConfig enumValueConfig in config.Values)
            {
                if (NativeType == null && enumValueConfig.Value != null)
                {
                    // TODO : what to do if:
                    // - values are not of the same type
                    // - one or more values are null
                    NativeType = enumValueConfig.Value.GetType();
                }

                EnumValue enumValue = new EnumValue(enumValueConfig);
                _nameToValues[enumValueConfig.Name] = enumValue;
                _valueToValues[enumValueConfig.Value] = enumValue;
            }

            SyntaxNode = config.SyntaxNode;
            Name = config.Name;
            Description = config.Description;
        }
        public EnumTypeDefinitionNode SyntaxNode { get; }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyCollection<EnumValue> Values => _nameToValues.Values;

        public Type NativeType { get; }

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

            throw new ArgumentException(
                "The specified value cannot be handled " +
                $"by the EnumType {Name}.");
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

        #region Configuration

        protected virtual void Configure(IEnumTypeDescriptor descriptor) { }

        #endregion

        #region TypeSystemNode

        ISyntaxNode IHasSyntaxNode.SyntaxNode => SyntaxNode;
        IEnumerable<ITypeSystemNode> ITypeSystemNode.GetNodes() => Values;

        #endregion
    }


}
