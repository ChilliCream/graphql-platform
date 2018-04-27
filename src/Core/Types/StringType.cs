using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class StringType
        : ScalarType
    {
        public StringType()
            : base("String")
        {
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal, Type targetType)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            if (literal is StringValueNode stringLiteral)
            {
                if (targetType == typeof(string)
                    || targetType == typeof(char[]))
                {
                    return stringLiteral.Value;
                }

                throw new NotSupportedException(
                    "The target type cannot be handled.");
            }

            throw new ArgumentException(
                "The string type can only parse string literals.",
                nameof(literal));
        }

        public override string Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (typeof(string).IsInstanceOfType(value))
            {
                return (string)value;
            }

            if (typeof(char).IsInstanceOfType(value))
            {
                return char.ToString((char)value);
            }

            if (typeof(char[]).IsInstanceOfType(value))
            {
                return string.Join(string.Empty, value);
            }

            throw new ArgumentException(
                "The specified value cannot be handled by the StringType.");
        }
    }
}
