using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class FloatTypeTests
        : NumberTypeTests<double, FloatType, FloatValueNode, double>
    {
        protected override FloatValueNode GetValueNode =>
            new FloatValueNode("1.000000E+000");

        protected override IValueNode GetWrongValueNode =>
            new StringValueNode("1");

        protected override double GetValue => 1.0d;

        protected override object GetWrongValue => 1.0m;

        protected override double GetAssertValue => 1.0d;
        protected override double GetSerializedAssertValue => 1.0d;

        protected override double GetMaxValue => double.MaxValue;
        protected override string GetAssertMaxValue => "1.797693E+308";

        protected override double GetMinValue => double.MinValue;
        protected override string GetAssertMinValue => "-1.797693E+308";

        [Fact]
        public void IsInstanceOfType_IntValueNode()
        {
            // arrange
            FloatType type = new FloatType();
            IntValueNode input = new IntValueNode("123");

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void ParseLiteral_IntValueNode()
        {
            // arrange
            FloatType type = new FloatType();
            IntValueNode input = new IntValueNode("123");

            // act
            object result = type.ParseLiteral(input);

            // assert
            Assert.IsType<double>(result);
            Assert.Equal(123d, result);
        }
    }
}
