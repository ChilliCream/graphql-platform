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
            var innerType = new StringType();

            // act
            var type = new NonNullType(innerType);

            // assert
            Assert.Equal(innerType, type.Type);
        }


        [Fact]
        public void EnsureNativeTypeIsCorrectlyDetected()
        {
            // act
            var type = new NonNullType(new StringType());

            // assert
            Assert.Equal(typeof(string), type.ClrType);
        }

        [Fact]
        public void EnsureInstanceOfIsDelegatedToInnerType()
        {
            // arrange
            var innerType = new ListType(new StringType());

            var type = new NonNullType(innerType);
            bool shouldBeFalse = ((IInputType)type).IsInstanceOfType(
                new IntValueNode("123"));
            bool shouldBeTrue = ((IInputType)type).IsInstanceOfType(
                new ListValueNode(new[] { new StringValueNode("foo") }));

            // assert
            Assert.False(shouldBeFalse);
            Assert.True(shouldBeTrue);
        }
    }
}
