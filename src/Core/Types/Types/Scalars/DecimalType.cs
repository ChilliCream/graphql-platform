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

        public override string Description =>
            TypeResources.DecimalType_Description();

        public override Type ClrType => typeof(decimal);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            return literal is FloatValueNode floatLiteral
                && TryParseDecimal(floatLiteral.Value, out _);
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is FloatValueNode floatValue
                && TryParseDecimal(floatValue.Value, out decimal d))
            {
                return d;
            }

            throw new ScalarSerializationException(
                TypeResources.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is decimal d)
            {
                return new FloatValueNode(SerializeDecimal(d));
            }

            throw new ScalarSerializationException(
                TypeResources.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is decimal d)
            {
                return d;
            }

            throw new ScalarSerializationException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is decimal d)
            {
                value = d;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryParseDecimal(string value, out decimal d) =>
            decimal.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out d);

        private static string SerializeDecimal(decimal value) =>
            value.ToString("E", CultureInfo.InvariantCulture);
    }
}
