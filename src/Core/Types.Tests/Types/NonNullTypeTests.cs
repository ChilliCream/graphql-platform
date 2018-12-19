using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class NonNullTypeTests
    {
        [Fact]
        public void EnsureInnerTypeIsCorrectlySet()
        {
            // arrange
            StringType innerType = new StringType();

            // act
            NonNullType type = new NonNullType(innerType);

            // assert
            Assert.Equal(innerType, type.Type);
        }


        [Fact]
        public void EnsureNativeTypeIsCorrectlyDetected()
        {
            // act
            NonNullType type = new NonNullType(new StringType());

            // assert
            Assert.Equal(typeof(string), type.ClrType);
        }

        [Fact]
        public void EnsureInstanceOfIsDelegatedToInnerType()
        {
            // arrange
            ListType innerType = new ListType(new StringType());

            NonNullType type = new NonNullType(innerType);
            bool shouldBeFalse = type.IsInstanceOfType(
                new IntValueNode("123"));
            bool shouldBeTrue = type.IsInstanceOfType(
                new ListValueNode(new[] { new StringValueNode("foo") }));

            // assert
            Assert.False(shouldBeFalse);
            Assert.True(shouldBeTrue);
        }
    }
}
