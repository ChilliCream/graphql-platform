using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class BooleanType
        : ScalarType
    {
        public BooleanType()
            : base("Boolean")
        {
        }

        public override Type NativeType { get; } = typeof(bool);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is BooleanValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is BooleanValueNode boolLiteral)
            {
                return boolLiteral.Value;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The boolean type can only parse bool literals.",
                nameof(literal));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (typeof(bool).IsInstanceOfType(value))
            {
                return value;
            }

            throw new ArgumentException(
                "The specified value cannot be handled by the BooleanType.");
        }
    }
}
