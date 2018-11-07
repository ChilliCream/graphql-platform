using System;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class DecimalType
        : NumberType<decimal, FloatValueNode>
    {
        public DecimalType()
            : base("Decimal")
        {
        }

        protected override decimal OnParseLiteral(FloatValueNode node) =>
            decimal.Parse(node.Value, NumberStyles.Float,
                CultureInfo.InvariantCulture);

        protected override FloatValueNode OnParseValue(decimal value) =>
            new FloatValueNode(
                value.ToString("E", CultureInfo.InvariantCulture));

        public override object Deserialize(object value)
        {
            if (value is null)
            {
                return null;
            }

            if (value is string s)
            {
                return decimal.Parse(s, NumberStyles.Float,
                    CultureInfo.InvariantCulture);
            }

            throw new ArgumentException(
                $"The specified value cannot be handled by the {Name}.");
        }
    }
}
