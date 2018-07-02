using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DecimalTypeTests
        : NumberTypeTests<decimal, DecimalType, FloatValueNode>
    {
        protected override FloatValueNode CreateValueNode() =>
            new FloatValueNode("1.000000E+000");

        protected override IValueNode CreateWrongValueNode() =>
            new IntValueNode("1");

        protected override decimal CreateValue() => 1.0m;

        protected override object CreateWrongValue() => 1.0d;

        protected override decimal AssertValue() => 1.0m;

        protected override decimal CreateMaxValue() => decimal.MaxValue;
        protected override string AssertMaxValue() => "7.922816E+028";

        protected override decimal CreateMinValue() => decimal.MinValue;
        protected override string AssertMinValue() => "-7.922816E+028";

    }
}
