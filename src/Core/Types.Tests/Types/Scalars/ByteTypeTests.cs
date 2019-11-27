using System.Globalization;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ByteTypeTests
        : NumberTypeTests<byte, ByteType, IntValueNode, byte>
    {
        protected override IntValueNode GetValueNode =>
            new IntValueNode("1");

        protected override IValueNode GetWrongValueNode =>
            new FloatValueNode("1.0f");

        protected override byte GetValue => 1;

        protected override object GetWrongValue => 1.0d;

        protected override byte GetAssertValue => 1;
        protected override byte GetSerializedAssertValue => 1;

        protected override byte GetMaxValue => byte.MaxValue;
        protected override string GetAssertMaxValue =>
            byte.MaxValue.ToString("D", CultureInfo.InvariantCulture);

        protected override byte GetMinValue => byte.MinValue;
        protected override string GetAssertMinValue =>
            byte.MinValue.ToString("D", CultureInfo.InvariantCulture);

        [Fact]
        public void Deserialize_Int_To_Byte()
        {
            // arrange
            var type = new ByteType();
            int serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((byte)123, Assert.IsType<byte>(value));
        }

        [Fact]
        public void Deserialize_NullableInt_To_Byte()
        {
            // arrange
            var type = new ByteType();
            int? serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((byte)123, Assert.IsType<byte>(value));
        }

        [Fact]
        public void Deserialize_Byte_To_Byte()
        {
            // arrange
            var type = new ByteType();
            byte serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.True(success);
            Assert.Equal((byte)123, Assert.IsType<byte>(value));
        }

        [Fact]
        public void Deserialize_Null_To_Null()
        {
            // arrange
            var type = new ByteType();

            // act
            bool success = type.TryDeserialize(null, out object value);

            // assert
            Assert.True(success);
            Assert.Null(value);
        }

        [Fact]
        public void Deserialize_Decimal_To_Byte()
        {
            // arrange
            var type = new ByteType();
            decimal serialized = 123;

            // act
            bool success = type.TryDeserialize(serialized, out object value);

            // assert
            Assert.False(success);
        }
    }
}
