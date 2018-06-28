using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class ListTypeTests
    {
        [Fact]
        public void EnsureElementTypeIsCorrectlySet()
        {
            // arrange
            StringType innerType = new StringType();

            // act
            ListType type = new ListType(innerType);

            // assert
            Assert.Equal(innerType, type.ElementType);
        }


        [Fact]
        public void EnsureNonNullElementTypeIsCorrectlySet()
        {
            // arrange
            NonNullType innerType = new NonNullType(new StringType());

            // act
            ListType type = new ListType(innerType);

            // assert
            Assert.Equal(innerType, type.ElementType);
        }

        [Fact]
        public void EnsureNativeTypeIsCorrectlyDetected()
        {
            // arrange
            NonNullType innerType = new NonNullType(new StringType());

            // act
            ListType type = new ListType(innerType);

            // assert
            Assert.Equal(typeof(string[]), type.NativeType);
        }

        [Fact]
        public void EnsureInstanceOfIsDelegatedToInnerType()
        {
            // arrange
            NonNullType innerType = new NonNullType(new StringType());

            // act
            ListType type = new ListType(innerType);
            bool shouldBeFalse = type.IsInstanceOfType(
                new ListValueNode(new[] { new NullValueNode() }));
            bool shouldBeTrue = type.IsInstanceOfType(
                new ListValueNode(new[] { new StringValueNode("foo") }));

            // assert
            Assert.False(shouldBeFalse);
            Assert.True(shouldBeTrue);
        }
    }
}
