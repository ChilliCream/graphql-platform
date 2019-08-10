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
            var type = new FloatType();
            var input = new IntValueNode("123");

            // act
            bool result = type.IsInstanceOfType(input);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void ParseLiteral_IntValueNode()
        {
            // arrange
            var type = new FloatType();
            var input = new IntValueNode("123");

            // act
            object result = type.ParseLiteral(input);

            // assert
            Assert.IsType<double>(result);
            Assert.Equal(123d, result);
        }

        [Fact]
        public void Deserialize_Int_To_Double()
        {
            // arrange
            var type = new FloatType();
            int serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((double)123, Assert.IsType<double>(value));
        }

        [Fact]
        public void Deserialize_NullableInt_To_Double()
        {
            // arrange
            var type = new FloatType();
            int? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((double)123, Assert.IsType<double>(value));
        }

        [Fact]
        public void Deserialize_Decimal_To_Double()
        {
            // arrange
            var type = new FloatType();
            decimal serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((double)123, Assert.IsType<double>(value));
        }

        [Fact]
        public void Deserialize_NullableDecimal_To_Double()
        {
            // arrange
            var type = new FloatType();
            decimal? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((double)123, Assert.IsType<double>(value));
        }

        [Fact]
        public void Deserialize_Double_To_Double()
        {
            // arrange
            var type = new FloatType();
            double serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((double)123, Assert.IsType<double>(value));
        }

        [Fact]
        public void Deserialize_Null_To_Null()
        {
            // arrange
            var type = new FloatType();

            // act
            bool success = type.TryDeserialize(null, out object value);

            // assert
            Assert.True(success);
            Assert.Null(value);
        }
    }
}
