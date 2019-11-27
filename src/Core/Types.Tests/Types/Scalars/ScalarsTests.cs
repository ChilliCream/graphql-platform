using System;
using System.Reflection;
using Xunit;

namespace HotChocolate.Types
{
    public class ScalarsTests
    {
        [InlineData(Foo.Bar, ValueKind.Enum)]
        [InlineData("foo", ValueKind.String)]
        [InlineData((short)1, ValueKind.Integer)]
        [InlineData((int)1, ValueKind.Integer)]
        [InlineData((long)1, ValueKind.Integer)]
        [InlineData((ushort)1, ValueKind.Integer)]
        [InlineData((uint)1, ValueKind.Integer)]
        [InlineData((ulong)1, ValueKind.Integer)]
        [InlineData((float)1, ValueKind.Float)]
        [InlineData((double)1, ValueKind.Float)]
        [InlineData(null, ValueKind.Null)]
        [Theory]
        public void TryGetKind(object value, ValueKind expectedKind)
        {
            // arrange
            // act
            bool isScalar = Scalars.TryGetKind(value, out ValueKind kind);

            // assert
            Assert.True(isScalar);
            Assert.Equal(expectedKind, kind);
        }

        [InlineData(Foo.Bar, ValueKind.Enum)]
        [InlineData((short)1, ValueKind.Integer)]
        [InlineData((int)1, ValueKind.Integer)]
        [InlineData((long)1, ValueKind.Integer)]
        [InlineData((ushort)1, ValueKind.Integer)]
        [InlineData((uint)1, ValueKind.Integer)]
        [InlineData((ulong)1, ValueKind.Integer)]
        [InlineData((float)1, ValueKind.Float)]
        [InlineData((double)1, ValueKind.Float)]
        [Theory]
        public void TryGetKind_From_Nullable(
            object value,
            ValueKind expectedKind)
        {
            // arrange
            Type type = typeof(Nullable<>).MakeGenericType(value.GetType());
            ConstructorInfo constructor =
                type.GetConstructor(new[] { value.GetType() });
            object nullableValue = constructor.Invoke(new[] { value });

            // act
            bool isScalar = Scalars.TryGetKind(
                nullableValue, out ValueKind kind);

            // assert
            Assert.True(isScalar);
            Assert.Equal(expectedKind, kind);
        }

        [Fact]
        public void Decimal_Is_Float()
        {
            // arrange
            decimal d = 123.123M;

            // act
            bool isScalar = Scalars.TryGetKind(d, out ValueKind kind);

            // assert
            Assert.True(isScalar);
            Assert.Equal(ValueKind.Float, kind);
        }

        [Fact]
        public void NullableDecimal_Is_Float()
        {
            // arrange
            decimal? d = 123.123M;

            // act
            bool isScalar = Scalars.TryGetKind(d, out ValueKind kind);

            // assert
            Assert.True(isScalar);
            Assert.Equal(ValueKind.Float, kind);
        }

        [Fact]
        public void Object_Is_Not_A_Serialized_Scalar()
        {
            // arrange
            object o = new object();

            // act
            bool isScalar = Scalars.TryGetKind(o, out _);

            // assert
            Assert.False(isScalar);
        }

        public enum Foo
        {
            Bar
        }
    }
}
