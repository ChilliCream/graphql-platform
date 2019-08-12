using System;
using System.Reflection;
using Xunit;

namespace HotChocolate.Types
{
    public class ScalarsTests
    {
        [InlineData(Foo.Bar, ScalarValueKind.Enum)]
        [InlineData("foo", ScalarValueKind.String)]
        [InlineData((short)1, ScalarValueKind.Integer)]
        [InlineData((int)1, ScalarValueKind.Integer)]
        [InlineData((long)1, ScalarValueKind.Integer)]
        [InlineData((ushort)1, ScalarValueKind.Integer)]
        [InlineData((uint)1, ScalarValueKind.Integer)]
        [InlineData((ulong)1, ScalarValueKind.Integer)]
        [InlineData((float)1, ScalarValueKind.Float)]
        [InlineData((double)1, ScalarValueKind.Float)]
        [InlineData(null, ScalarValueKind.Null)]
        [Theory]
        public void TryGetKind(object value, ScalarValueKind expectedKind)
        {
            // arrange
            // act
            bool isScalar = Scalars.TryGetKind(value, out ScalarValueKind kind);

            // assert
            Assert.True(isScalar);
            Assert.Equal(expectedKind, kind);
        }

        [InlineData(Foo.Bar, ScalarValueKind.Enum)]
        [InlineData((short)1, ScalarValueKind.Integer)]
        [InlineData((int)1, ScalarValueKind.Integer)]
        [InlineData((long)1, ScalarValueKind.Integer)]
        [InlineData((ushort)1, ScalarValueKind.Integer)]
        [InlineData((uint)1, ScalarValueKind.Integer)]
        [InlineData((ulong)1, ScalarValueKind.Integer)]
        [InlineData((float)1, ScalarValueKind.Float)]
        [InlineData((double)1, ScalarValueKind.Float)]
        [Theory]
        public void TryGetKind_From_Nullable(
            object value,
            ScalarValueKind expectedKind)
        {
            // arrange
            Type type = typeof(Nullable<>).MakeGenericType(value.GetType());
            ConstructorInfo constructor =
                type.GetConstructor(new[] { value.GetType() });
            object nullableValue = constructor.Invoke(new[] { value });

            // act
            bool isScalar = Scalars.TryGetKind(
                nullableValue, out ScalarValueKind kind);

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
            bool isScalar = Scalars.TryGetKind(d, out ScalarValueKind kind);

            // assert
            Assert.True(isScalar);
            Assert.Equal(ScalarValueKind.Float, kind);
        }

        [Fact]
        public void NullableDecimal_Is_Float()
        {
            // arrange
            decimal? d = 123.123M;

            // act
            bool isScalar = Scalars.TryGetKind(d, out ScalarValueKind kind);

            // assert
            Assert.True(isScalar);
            Assert.Equal(ScalarValueKind.Float, kind);
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
