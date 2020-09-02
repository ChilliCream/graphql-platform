using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a GraphQL input object type
    /// </summary>
    public partial class InputObjectType
        : NamedTypeBase<InputObjectTypeDefinition>
        , IInputObjectType
    {
        public override TypeKind Kind => TypeKind.InputObject;

        /// <summary>
        /// Gets the GraphQL syntax representation of this type
        /// if it was provided during initialization.
        /// </summary>
        public InputObjectTypeDefinitionNode? SyntaxNode { get; private set; }

        /// <summary>
        /// Gets the fields of this type.
        /// </summary>
        public FieldCollection<InputField> Fields { get; private set; } = default!;

        IFieldCollection<IInputField> IInputObjectType.Fields => Fields;

        /// <inheritdoc />
        public virtual bool IsInstanceOfType(IValueNode literal)
        {
            if (literal is null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is ObjectValueNode
                || literal is NullValueNode;
        }

        /// <inheritdoc />
        public virtual bool IsInstanceOfType(object? value)
        {
            if (value is null)
            {
                return true;
            }

            return RuntimeType.IsInstanceOfType(value);
        }

        /// <inheritdoc />
        public virtual object? ParseLiteral(IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is ObjectValueNode objectValueSyntax)
            {
                return _parseLiteral(objectValueSyntax);
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            throw new SerializationException(
                TypeResources.InputObjectType_CannotParseLiteral,
                this);
        }

        /// <inheritdoc />
        public virtual IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            return _objectToValueConverter.Convert(this, runtimeValue);
        }

        public IValueNode ParseResult(object? resultValue)
        {
            if (resultValue is null)
            {
                return NullValueNode.Default;
            }

            if (resultValue is IReadOnlyDictionary<string, object> dict)
            {
                var list = new List<ObjectFieldNode>();

                foreach (InputField field in Fields)
                {
                    if(dict.TryGetValue(field.Name.Value, out object? value))
                    {
                        list.Add(new ObjectFieldNode(
                            field.Name.Value, 
                            field.Type.ParseResult(value)));
                    }
                }

                return new ObjectValueNode(list);
            }

            if (RuntimeType != typeof(object) && RuntimeType.IsInstanceOfType(resultValue))
            {
                return ParseValue(resultValue);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseResult(Name, resultValue.GetType()),
                this);
        }

        public object? Serialize(object? runtimeValue)
        {
            if (TrySerialize(runtimeValue, out object? serialized))
            {
                return serialized;
            }

            throw new SerializationException(
                "The specified value is not a valid input object.",
                this);
        }

        public virtual bool TrySerialize(object? runtimeValue, out object? resultValue)
        {
            try
            {
                if (runtimeValue is null)
                {
                    resultValue = null;
                    return true;
                }

                if (runtimeValue is IReadOnlyDictionary<string, object> ||
                    runtimeValue is IDictionary<string, object>)
                {
                    resultValue = runtimeValue;
                    return true;
                }

                resultValue = _objectToDictionary.Convert(this, runtimeValue);
                return true;
            }
            catch
            {
                resultValue = null;
                return false;
            }
        }

        public object? Deserialize(object? resultValue)
        {
            if (TryDeserialize(resultValue, out object? deserialized))
            {
                return deserialized;
            }

            throw new SerializationException(
                "The specified value is not a serialized input object.",
                this);
        }

        public virtual bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            try
            {
                if (resultValue is null)
                {
                    runtimeValue = null;
                    return true;
                }

                if (resultValue is IReadOnlyDictionary<string, object> dict)
                {
                    runtimeValue = _deserialize(dict);
                    return true;
                }

                if (RuntimeType != typeof(object) && RuntimeType.IsInstanceOfType(resultValue))
                {
                    runtimeValue = resultValue;
                    return true;
                }

                runtimeValue = null;
                return false;
            }
            catch
            {
                runtimeValue = null;
                return false;
            }
        }
    }
}
