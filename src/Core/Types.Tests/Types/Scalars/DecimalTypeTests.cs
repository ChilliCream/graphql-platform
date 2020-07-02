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
        protected override string GetAssertMaxValue => "79228162514264337593543950335";

        protected override decimal GetMinValue => decimal.MinValue;
        protected override string GetAssertMinValue => "-79228162514264337593543950335";

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

        [Fact]
        public void Deserialize_Int_To_Decimal()
        {
            // arrange
            var type = new DecimalType();
            int serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((decimal)123, Assert.IsType<decimal>(value));
        }

        [Fact]
        public void Deserialize_NullableInt_To_Decimal()
        {
            // arrange
            var type = new DecimalType();
            int? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((decimal)123, Assert.IsType<decimal>(value));
        }

        [Fact]
        public void Deserialize_Float_To_Decimal()
        {
            // arrange
            var type = new DecimalType();
            float serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((decimal)123, Assert.IsType<decimal>(value));
        }

        [Fact]
        public void Deserialize_NullableFloat_To_Decimal()
        {
            // arrange
            var type = new DecimalType();
            float? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((decimal)123, Assert.IsType<decimal>(value));
        }

        [Fact]
        public void Deserialize_Decimal_To_Decimal()
        {
            // arrange
            var type = new DecimalType();
            decimal serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((decimal)123, Assert.IsType<decimal>(value));
        }

        [Fact]
        public void Deserialize_Null_To_Null()
        {
            // arrange
            var type = new DecimalType();

            // act
            bool success = type.TryDeserialize(null, out object value);

            // assert
            Assert.True(success);
            Assert.Null(value);
        }

        [Fact]
        public void ParseValue_HandlesMoreThan6Digits()
        {
            // arrange
            var type = new DecimalType();
            var input = 1234567.1234567m;
            var output = "1234567.1234567";

            // act
            var result = type.ParseValue(input);

            // assert
            Assert.True(result is FloatValueNode);
            Assert.True(result.Value is string);
            Assert.Equal(output, (string)result.Value);
        }

        [Fact]
        public void ParseValue_FormatsToDefaultSignificantDigits()
        {
            // arrange
            var type = new DecimalType();
            var input = 1234567.891123456789m;
            var output = "1234567.891123456789";

            // act
            var result = type.ParseValue(input);

            // assert
            Assert.True(result is FloatValueNode);
            Assert.True(result.Value is string);
            Assert.Equal(output, (string)result.Value);
        }

        [Fact]
        public void ParseValue_Handle12Digits()
        {
            // arrange
            var type = new DecimalType();
            var input = 1234567.890123456789m;
            var output = "1234567.890123456789";

            // act
            var result = type.ParseValue(input);

            // assert
            Assert.True(result is FloatValueNode);
            Assert.True(result.Value is string);
            Assert.Equal(output, (string)result.Value);
        }

        [Fact]
        public void ParseValue_FormatsToSpecifiedNumberOfDecimalDigitsLong()
        {
            // arrange
            var type = new DecimalType();
            var input = 1234567.890123456789m;
            var output = "1234567.890123456789";

            // act
            var result = type.ParseValue(input);

            // assert
            Assert.True(result is FloatValueNode);
            Assert.True(result.Value is string);
            Assert.Equal(output, (string)result.Value);
        }
    }
}
