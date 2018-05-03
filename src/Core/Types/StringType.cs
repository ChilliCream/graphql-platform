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

        public override Type NativeType { get; } = typeof(string);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is StringValueNode
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

            throw new ArgumentException(
                "The specified value cannot be handled by the StringType.");
        }
    }
}
