using System;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class DecimalType
        : ScalarType
    {
        public DecimalType()
            : base("Decimal")
        {
        }

        public override Type NativeType { get; } = typeof(decimal);

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
                return decimal.Parse(floatLiteral.Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                "The decimal type can only parse float literals.",
                nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode();
            }

            if (value is decimal d)
            {
                return new FloatValueNode(d.ToString("e",
                    CultureInfo.InvariantCulture));
            }

            throw new ArgumentException(
                "The specified value has to be an decimal" +
                "to be parsed by the decimal type.");
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is decimal)
            {
                return value;
            }

            throw new ArgumentException(
                "The specified value cannot be handled by the DecimalType.");
        }
    }
}
