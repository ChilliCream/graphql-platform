using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class FloatType
        : ScalarType
    {
        public FloatType()
            : base("Float")
        {
        }

        public override Type NativeType { get; } = typeof(double);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is FloatValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is FloatValueNode floatLiteral)
            {
                return double.Parse(floatLiteral.Value);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The int type can only parse int literals.",
                nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode();
            }

            if (value is double d)
            {
                return new FloatValueNode(d.ToString("e"));
            }

            if (value is float f)
            {
                return new FloatValueNode(f.ToString("e"));
            }

            throw new ArgumentException(
                "The specified value has to be an integer" +
                "to be parsed by the int type.");
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is double || value is float)
            {
                return value;
            }

            throw new ArgumentException(
                "The specified value cannot be handled by the IntType.");
        }
    }
}
