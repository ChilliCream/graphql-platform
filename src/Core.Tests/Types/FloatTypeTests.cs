using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class FloatTypeTests
        : NumberTypeTests<double, FloatType, FloatValueNode>
    {
        protected override FloatValueNode CreateValueNode() =>
            new FloatValueNode("1.000000E+000");

        protected override IValueNode CreateWrongValueNode() =>
            new IntValueNode("1");

        protected override double CreateValue() => 1.0d;

        protected override object CreateWrongValue() => 1.0m;

        protected override double AssertValue() => 1.0d;

        protected override double CreateMaxValue() => double.MaxValue;
        protected override string AssertMaxValue() => "1.797693E+308";

        protected override double CreateMinValue() => double.MinValue;
        protected override string AssertMinValue() => "-1.797693E+308";

        [Fact]
        public void ParseValue_Float_Max()
        {
            // arrange
            FloatType type = new FloatType();
            float input = float.MaxValue;

            // act
            FloatValueNode literal =
                (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal("3.402823E+038", literal.Value);
        }

        [Fact]
        public void ParseValue_Float_Min()
        {
            // arrange
            FloatType type = new FloatType();
            float input = float.MinValue;

            // act
            FloatValueNode literal =
                (FloatValueNode)type.ParseValue(input);

            // assert
            Assert.Equal("-3.402823E+038", literal.Value);
        }
    }
}
