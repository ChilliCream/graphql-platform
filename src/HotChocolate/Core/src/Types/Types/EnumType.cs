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
    public partial class EnumType
        : NamedTypeBase<EnumTypeDefinition>
        , ILeafType
    {
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

        public bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            if (literal is EnumValueNode ev)
            {
                return _nameToValues.ContainsKey(ev.Value);
            }

            return false;
        }

        public object ParseLiteral(IValueNode literal)
        {
            if (literal is null)
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

            return RuntimeType.IsInstanceOfType(value);
        }

        public object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            throw new NotImplementedException();
        }

        public IValueNode ParseValue(object value)
        {
            if (value is null)
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

        public IValueNode ParseResult(object? resultValue)
        {
            throw new NotImplementedException();
        }

        public object Serialize(object runtimeValue)
        {
            if (runtimeValue is null)
            {
                return null;
            }

            if (RuntimeType.IsInstanceOfType(runtimeValue)
                && _valueToValues.TryGetValue(runtimeValue, out EnumValue enumValue))
            {
                return enumValue.Name;
            }

            // schema first unbound enum type
            if (RuntimeType == typeof(object)
                && _nameToValues.TryGetValue(
                    runtimeValue.ToString().ToUpperInvariant(),
                    out enumValue))
            {
                return enumValue.Name;
            }

            throw new ArgumentException(
                TypeResourceHelper.Scalar_Cannot_Serialize(Name));
        }

        public object Deserialize(object resultValue)
        {
            if (TryDeserialize(resultValue, out object v))
            {
                return v;
            }

            throw new ArgumentException(
                TypeResourceHelper.Scalar_Cannot_Deserialize(Name));
        }

        public bool TryDeserialize(object resultValue, out object runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string name
                && _nameToValues.TryGetValue(name, out EnumValue enumValue))
            {
                runtimeValue = enumValue.Value;
                return true;
            }

            if (_valueToValues.TryGetValue(resultValue, out enumValue))
            {
                runtimeValue = enumValue.Value;
                return true;
            }

            runtimeValue = null;
            return false;
        }

    }
}
