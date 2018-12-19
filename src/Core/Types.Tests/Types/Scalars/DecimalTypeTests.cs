using System;
using System.Globalization;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DecimalTypeTests
        : NumberTypeTests<decimal, DecimalType, StringValueNode, string>
    {
        protected override StringValueNode GetValueNode =>
            new StringValueNode("1.000000E+000");

        protected override IValueNode GetWrongValueNode =>
            new IntValueNode("1");

        protected override decimal GetValue => 1.0m;

        protected override object GetWrongValue => 1.0d;

        protected override decimal GetAssertValue => 1.0m;
        protected override string GetSerializedAssertValue =>
            ((decimal)1.0m).ToString("E", CultureInfo.InvariantCulture);

        protected override decimal GetMaxValue => decimal.MaxValue;
        protected override string GetAssertMaxValue => "7.922816E+028";

        protected override decimal GetMinValue => decimal.MinValue;
        protected override string GetAssertMinValue => "-7.922816E+028";

    }
}
