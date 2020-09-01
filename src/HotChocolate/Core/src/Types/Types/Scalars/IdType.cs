using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The ID scalar type represents a unique identifier, often used to refetch
    /// an object or as the key for a cache. The ID type is serialized in the
    /// same way as a String; however, it is not intended to be human‐readable.
    ///
    /// While it is often numeric, it should always serialize as a String.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-ID
    /// </summary>
    [SpecScalar]
    public sealed class IdType
        : ScalarType<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdType"/> class.
        /// </summary>
        public IdType()
            : base(ScalarNames.ID, BindingBehavior.Explicit)
        {
            Description = TypeResources.IdType_Description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdType"/> class.
        /// </summary>
        public IdType(NameString name)
            : base(name, BindingBehavior.Explicit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdType"/> class.
        /// </summary>
        public IdType(NameString name, string description)
            : base(name, BindingBehavior.Explicit)
        {
            Description = description;
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode
                || literal is IntValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is StringValueNode stringLiteral)
            {
                return stringLiteral.Value;
            }

            if (literal is IntValueNode intLiteral)
            {
                return intLiteral.Value;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is string s)
            {
                return new StringValueNode(s);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is string)
            {
                serialized = value;
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object resultValue, out object runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is string)
            {
                runtimeValue = resultValue;
                return true;
            }

            if (TryConvertSerialized(resultValue, ValueKind.Integer, out string c))
            {
                runtimeValue = c;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
