using HotChocolate.Language;

namespace HotChocolate.Types
{
    public class LongTypeTests
        : NumberTypeTests<long, LongType, IntValueNode>
    {
        protected override IntValueNode CreateValueNode() =>
            new IntValueNode("1");

        protected override IValueNode CreateWrongValueNode() =>
            new FloatValueNode("1.0f");

        protected override long CreateValue() => 1L;

        protected override object CreateWrongValue() => 1.0d;

        protected override long AssertValue() => 1L;

        protected override long CreateMaxValue() => long.MaxValue;
        protected override string AssertMaxValue() => "9223372036854775807";

        protected override long CreateMinValue() => long.MinValue;
        protected override string AssertMinValue() => "-9223372036854775808";
    }
}
