using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class LongTypeTests
        : NumberTypeTests<long, LongType, IntValueNode>
    {
        protected override IntValueNode GetValueNode =>
            new IntValueNode("1");

        protected override IValueNode GetWrongValueNode =>
            new FloatValueNode("1.0f");

        protected override long GetValue => 1L;

        protected override object GetWrongValue => 1.0d;

        protected override long GetAssertValue => 1L;

        protected override long GetMaxValue => long.MaxValue;
        protected override string GetAssertMaxValue => "9223372036854775807";

        protected override long GetMinValue => long.MinValue;
        protected override string GetAssertMinValue => "-9223372036854775808";
    }
}
