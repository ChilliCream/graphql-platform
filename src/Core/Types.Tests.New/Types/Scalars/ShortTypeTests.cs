using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class ShortTypeTests
        : NumberTypeTests<short, ShortType, IntValueNode, short>
    {
        protected override IntValueNode GetValueNode =>
            new IntValueNode("1");

        protected override IValueNode GetWrongValueNode =>
            new FloatValueNode("1.0f");

        protected override short GetValue => 1;

        protected override object GetWrongValue => 1.0d;

        protected override short GetAssertValue => 1;
        protected override short GetSerializedAssertValue => 1;

        protected override short GetMaxValue => short.MaxValue;
        protected override string GetAssertMaxValue =>
            short.MaxValue.ToString("D", CultureInfo.InvariantCulture);

        protected override short GetMinValue => short.MinValue;
        protected override string GetAssertMinValue =>
            short.MinValue.ToString("D", CultureInfo.InvariantCulture);
    }
}
