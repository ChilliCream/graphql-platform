using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class IntType
        : ScalarType
    {
        public IntType()
            : base("Int")
        {
        }

        public override Type NativeType { get; } = typeof(int);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is IntValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is IntValueNode boolLiteral)
            {
                return boolLiteral.Value;
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The int type can only parse int literals.",
                nameof(literal));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (typeof(int).IsInstanceOfType(value))
            {
                return value;
            }

            throw new ArgumentException(
                "The specified value cannot be handled by the IntType.");
        }
    }
}
