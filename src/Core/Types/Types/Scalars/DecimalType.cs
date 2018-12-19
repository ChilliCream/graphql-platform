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

        public override Type ClrType => typeof(decimal);

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
                return ParseDecimal(stringLiteral.Value);
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
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is decimal d)
            {
                return new StringValueNode(SerializeDecimal(d));
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

            if (value is decimal d)
            {
                return SerializeDecimal(d);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s)
            {
                value = ParseDecimal(s);
                return true;
            }

            value = null;
            return false;
        }

        private static decimal ParseDecimal(string value) =>
           decimal.Parse(
               value,
               NumberStyles.Float,
               CultureInfo.InvariantCulture);

        private static string SerializeDecimal(decimal value) =>
            value.ToString("E", CultureInfo.InvariantCulture);
    }
}
