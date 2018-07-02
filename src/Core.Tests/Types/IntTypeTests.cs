using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class IntTypeTests
        : NumberTypeTests<int, IntType, IntValueNode>
    {
        protected override IntValueNode CreateValueNode() =>
            new IntValueNode("1");

        protected override IValueNode CreateWrongValueNode() =>
            new FloatValueNode("1.0f");

        protected override int CreateValue() => 1;

        protected override object CreateWrongValue() => 1.0d;

        protected override int AssertValue() => 1;

        protected override int CreateMaxValue() => int.MaxValue;
        protected override string AssertMaxValue() => "2147483647";

        protected override int CreateMinValue() => int.MinValue;
        protected override string AssertMinValue() => "-2147483648";
    }
}
