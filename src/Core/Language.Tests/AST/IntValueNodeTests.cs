using Xunit;

namespace HotChocolate.Language
{
    public class IntValueNodeTests
    {
        [InlineData(1)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(10000)]
        [InlineData(-10000)]
        [Theory]
        public void ValueNode_Equals(int value)
        {
            // arrange
            IValueNode a = new IntValueNode(value);
            IValueNode b = new IntValueNode(value);

            // act
            bool result = a.Equals(b);

            // assert
            Assert.True(result);
        }

        [InlineData(1, 2)]
        [InlineData(-1, -2)]
        [InlineData(0, 1)]
        [InlineData(10000, -5)]
        [InlineData(-10000, 45)]
        [Theory]
        public void ValueNode_NotEquals(int avalue, int bvalue)
        {
            // arrange
            IValueNode a = new IntValueNode(avalue);
            IValueNode b = new IntValueNode(bvalue);

            // act
            bool result = a.Equals(b);

            // assert
            Assert.False(result);
        }
    }
}
