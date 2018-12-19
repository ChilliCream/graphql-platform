using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class IntTypeTests
        : NumberTypeTests<int, IntType, IntValueNode, int>
    {
        protected override IntValueNode GetValueNode =>
            new IntValueNode("1");

        protected override IValueNode GetWrongValueNode =>
            new FloatValueNode("1.0f");

        protected override int GetValue => 1;

        protected override object GetWrongValue => 1.0d;

        protected override int GetAssertValue => 1;
        protected override int GetSerializedAssertValue => 1;

        protected override int GetMaxValue => int.MaxValue;
        protected override string GetAssertMaxValue => "2147483647";

        protected override int GetMinValue => int.MinValue;
        protected override string GetAssertMinValue => "-2147483648";

    }
}
