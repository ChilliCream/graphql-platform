using Xunit;

namespace HotChocolate.Language
{
    public class ValueNodeExtensionsTests
    {
        [Fact]
        public static void IsNull_Null_True()
        {
            // arrange
            IValueNode value = default(IValueNode);

            // act
            bool result = value.IsNull();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsNull_NullValueNode_True()
        {
            // arrange
            IValueNode value = NullValueNode.Default;

            // act
            bool result = value.IsNull();

            // assert
            Assert.True(result);
        }

        [Fact]
        public static void IsNull_StringValueNode_False()
        {
            // arrange
            IValueNode value = new StringValueNode("foo");

            // act
            bool result = value.IsNull();

            // assert
            Assert.False(result);
        }
    }
}
