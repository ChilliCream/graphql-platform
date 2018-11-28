using System;
using System.Globalization;
using HotChocolate.Language;

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
        : ScalarType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringType"/> class.
        /// </summary>
        public IdType()
            : base("ID")
        {
        }

        public override string Description =>
            TypeResources.IdType_Description();

        public override Type ClrType => typeof(string);

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

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()),
                nameof(literal));
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

            if (value is char c)
            {
                return new StringValueNode(c.ToString());
            }

            if (value is int i)
            {
                return new IntValueNode(i);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseValue(
                    Name, value.GetType()),
                nameof(value));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is string s)
            {
                return s;
            }

            if (value is char c)
            {
                return c.ToString(CultureInfo.InvariantCulture);
            }

            if (value is int i)
            {
                return i.ToString(CultureInfo.InvariantCulture);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        public override object Deserialize(object value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is string)
            {
                return value;
            }

            if (value is int i)
            {
                return i.ToString(CultureInfo.InvariantCulture);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }
    }
}
