using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class LongTypeTests
        : NumberTypeTests<long, LongType, IntValueNode, long>
    {
        protected override IntValueNode GetValueNode =>
            new IntValueNode("1");

        protected override IValueNode GetWrongValueNode =>
            new FloatValueNode("1.0f");

        protected override long GetValue => 1L;

        protected override object GetWrongValue => 1.0d;

        protected override long GetAssertValue => 1L;
        protected override long GetSerializedAssertValue => 1L;

        protected override long GetMaxValue => long.MaxValue;
        protected override string GetAssertMaxValue => "9223372036854775807";

        protected override long GetMinValue => long.MinValue;
        protected override string GetAssertMinValue => "-9223372036854775808";

        [Fact]
        public void Deserialize_Int_To_Long()
        {
            // arrange
            var type = new LongType();
            int serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((long)123, Assert.IsType<long>(value));
        }

        [Fact]
        public void Deserialize_NullableInt_To_Long()
        {
            // arrange
            var type = new LongType();
            int? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((long)123, Assert.IsType<long>(value));
        }

        [Fact]
        public void Deserialize_Decimal_To_Long()
        {
            // arrange
            var type = new LongType();
            decimal serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Deserialize_NullableDecimal_To_Long()
        {
            // arrange
            var type = new LongType();
            decimal? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.False(success);
        }

        [Fact]
        public void Deserialize_Long_To_Long()
        {
            // arrange
            var type = new LongType();
            long serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((long)123, Assert.IsType<long>(value));
        }

        [Fact]
        public void Deserialize_Null_To_Null()
        {
            // arrange
            var type = new LongType();

            // act
            bool success = type.TryDeserialize(null, out object value);

            // assert
            Assert.True(success);
            Assert.Null(value);
        }
    }
}
