using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DecimalTypeTests
        : NumberTypeTests<decimal, DecimalType, FloatValueNode, decimal>
    {
        protected override FloatValueNode GetValueNode =>
            new FloatValueNode("1.000000E+000");

        protected override IValueNode GetWrongValueNode =>
            new StringValueNode("1");

        protected override decimal GetValue => 1.0m;

        protected override object GetWrongValue => 1.0d;

        protected override decimal GetAssertValue => 1.0m;
        protected override decimal GetSerializedAssertValue => ((decimal)1.0m);

        protected override decimal GetMaxValue => decimal.MaxValue;
        protected override string GetAssertMaxValue => "7.922816E+028";

        protected override decimal GetMinValue => decimal.MinValue;
        protected override string GetAssertMinValue => "-7.922816E+028";

        [Fact]
        public void IsInstanceOfType_IntLiteral_True()
        {
            // arrange
            var type = new DecimalType();
            var input = new IntValueNode("123");

            // act
            var result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void ParseLiteral_IntValueNode_decimal()
        {
            // arrange
            var type = new DecimalType();
            var input = new IntValueNode("123");

            // act
            var output = type.ParseLiteral(input);

            // assert
            Assert.Equal(123, Assert.IsType<decimal>(output));
        }

    }
}
