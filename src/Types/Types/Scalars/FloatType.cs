using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public sealed class FloatType
        : NumberType<double, FloatValueNode>
    {
        public FloatType()
            : base("Float")
        {
        }

        protected override double OnParseLiteral(FloatValueNode node) =>
            double.Parse(node.Value, NumberStyles.Float, CultureInfo.InvariantCulture);

        protected override FloatValueNode OnParseValue(double value) =>
            new FloatValueNode(value.ToString("E", CultureInfo.InvariantCulture));

        protected override IEnumerable<Type> AdditionalTypes =>
             new[] { typeof(float) };
    }
}
