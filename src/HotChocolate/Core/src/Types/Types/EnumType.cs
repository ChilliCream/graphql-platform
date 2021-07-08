using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// GraphQL Enum types, like Scalar types, also represent leaf values in a GraphQL type system.
    /// However Enum types describe the set of possible values.
    ///
    /// Enums are not references for a numeric value, but are unique values in their own right.
    /// They may serialize as a string: the name of the represented value.
    ///
    /// In this example, an Enum type called Direction is defined:
    ///
    /// <code>
    /// enum Direction {
    ///   NORTH
    ///   EAST
    ///   SOUTH
    ///   WEST
    /// }
    /// </code>
    /// </summary>
    public partial class EnumType
        : NamedTypeBase<EnumTypeDefinition>
        , IEnumType
    {
        /// <inheritdoc />
        public override TypeKind Kind => TypeKind.Enum;

        /// <summary>
        /// Gets the associated syntax node from the GraphQL SDL.
        /// </summary>
        public EnumTypeDefinitionNode? SyntaxNode { get; private set; }

        /// <summary>
        /// Gets the enum values of this type.
        /// </summary>
        public IReadOnlyCollection<IEnumValue> Values => _enumValues.Values;

        /// <summary>
        /// Gets a dictionary that allows to lookup the enum value by its name.
        /// </summary>
        protected IReadOnlyDictionary<NameString, IEnumValue> NameLookup => _enumValues;

        /// <summary>
        /// Gets a dictionary that allows to lookup the enum value by its runtime value.
        /// </summary>
        protected IReadOnlyDictionary<object, IEnumValue> ValueLookup => _valueLookup;

        /// <summary>
        /// Tries to resolve the runtime value of this enum type by its name.
        /// </summary>
        /// <param name="name">
        /// The GraphQL name for the enum value.
        /// </param>
        /// <param name="runtimeValue">
        /// The runtime enum runtime value.
        /// </param>
        /// <returns>
        /// <c>true</c>, if there is a runtime value associated with the provided enum value name.
        /// </returns>
        public bool TryGetRuntimeValue(
            NameString name,
            [NotNullWhen(true)] out object? runtimeValue)
        {
            if (_enumValues.TryGetValue(name, out IEnumValue? value))
            {
                runtimeValue = value.Value;
                return true;
            }

            runtimeValue = null;
            return false;
        }

        /// <inheritdoc />
        public bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is NullValueNode)
            {
                return true;
            }

            if (valueSyntax is EnumValueNode ev)
            {
                return _enumValues.ContainsKey(ev.Value);
            }

            if (valueSyntax is StringValueNode sv)
            {
                return _enumValues.ContainsKey(sv.Value);
            }

            return false;
        }

        /// <inheritdoc />
        public bool IsInstanceOfType(object? runtimeValue)
        {
            return runtimeValue is null ||
                RuntimeType.IsInstanceOfType(runtimeValue);
        }

        /// <inheritdoc />
        public object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is EnumValueNode evn &&
                _enumValues.TryGetValue(evn.Value, out IEnumValue? ev))
            {
                return ev.Value;
            }

            if (valueSyntax is StringValueNode svn &&
                _enumValues.TryGetValue(svn.Value, out ev))
            {
                return ev.Value;
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
                this);
        }

        /// <inheritdoc />
        public IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            if (_valueLookup.TryGetValue(runtimeValue, out IEnumValue? enumValue))
            {
                return new EnumValueNode(enumValue.Name);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(Name, runtimeValue.GetType()),
                this);
        }

        /// <inheritdoc />
        public IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is string s &&
                _enumValues.TryGetValue(s, out IEnumValue? enumValue))
            {
                return new EnumValueNode(enumValue.Name);
            }

            if (resultValue is NameString n &&
                _enumValues.TryGetValue(n, out enumValue))
            {
                return new EnumValueNode(enumValue.Name);
            }

            if (_valueLookup.TryGetValue(resultValue, out enumValue))
            {
                return new EnumValueNode(enumValue.Name);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
                this);
        }

        /// <inheritdoc />
        public object? Serialize(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return null;
            }

            if (RuntimeType.IsInstanceOfType(runtimeValue) &&
                _valueLookup.TryGetValue(runtimeValue, out IEnumValue? enumValue))
            {
                return enumValue.Name;
            }

            // schema first unbound enum type
            if (RuntimeType == typeof(object))
            {
                string name = _naming.GetEnumValueName(runtimeValue);
                if (_enumValues.TryGetValue(name, out enumValue))
                {
                    return enumValue.Name;
                }
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_Serialize(Name),
                this);
        }

        /// <inheritdoc />
        public object? Deserialize(object? resultValue)
        {
            if (TryDeserialize(resultValue, out object? runtimeValue))
            {
                return runtimeValue;
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_Deserialize(Name),
                this);
        }

        /// <inheritdoc />
        public bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string s &&
                _enumValues.TryGetValue(s, out IEnumValue? enumValue))
            {
                runtimeValue = enumValue.Value;
                return true;
            }

            if (resultValue is NameString n &&
                _enumValues.TryGetValue(n, out enumValue))
            {
                runtimeValue = enumValue.Value;
                return true;
            }

            if (_valueLookup.TryGetValue(resultValue, out enumValue))
            {
                runtimeValue = enumValue.Value;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
