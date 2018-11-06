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
    public sealed class IdType
        : StringTypeBase
    {
        public IdType()
            : base("ID")
        {
        }

        public override Type ClrType { get; } = typeof(string);

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
                $"The {Name} type can only parse string literals.",
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
                "The specified value has to be a string or char in order " +
                $"to be parsed by the {Name} type.");
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
                "The specified value cannot be serialized by the " +
                $"{Name} type.");
        }
    }
}
