using System;
using System.Globalization;
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
                return double.Parse(floatLiteral.Value,
                    NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                $"The {nameof(FloatType)} can only parse float literals.",
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
                return new FloatValueNode(d.ToString("e",
                    CultureInfo.InvariantCulture));
            }

            if (value is float f)
            {
                return new FloatValueNode(f.ToString("e",
                    CultureInfo.InvariantCulture));
            }

            throw new ArgumentException(
                $"The specified value has to be a {nameof(System.Single)} " +
                $"or a {nameof(System.Double)} to be parsed " +
                $"by the {nameof(FloatType)}.");
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
                "The specified value cannot be handled by the " +
                $"{nameof(FloatType)}.");
        }
    }
}
