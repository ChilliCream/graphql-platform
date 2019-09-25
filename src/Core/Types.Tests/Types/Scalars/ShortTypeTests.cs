using System.Globalization;
using HotChocolate.Language;
using Xunit;

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

        [Fact]
        public void Deserialize_Int_To_Short()
        {
            // arrange
            var type = new ShortType();
            int serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((short)123, Assert.IsType<short>(value));
        }

        [Fact]
        public void Deserialize_NullableInt_To_Short()
        {
            // arrange
            var type = new ShortType();
            int? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((short)123, Assert.IsType<short>(value));
        }

        [Fact]
        public void Deserialize_Decimal_To_Short()
        {
            // arrange
            var type = new ShortType();
            decimal serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Deserialize_NullableDecimal_To_Short()
        {
            // arrange
            var type = new ShortType();
            decimal? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Deserialize_Short_To_Short()
        {
            // arrange
            var type = new ShortType();
            short serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((short)123, Assert.IsType<short>(value));
        }

        [Fact]
        public void Deserialize_Null_To_Null()
        {
            // arrange
            var type = new ShortType();

            // act
            bool success = type.TryDeserialize(null, out object value);

            // assert
            Assert.True(success);
            Assert.Null(value);
        }
    }
}
