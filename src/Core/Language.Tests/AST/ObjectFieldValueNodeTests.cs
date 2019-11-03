using Xunit;

namespace HotChocolate.Language
{
    public class ObjectFieldValueNodeTests
    {
        [Fact]
        public void Create_Float()
        {
            // arrange
            // act
            var obj = new ObjectFieldNode("abc", 1.2);

            // assert
            Assert.Equal("abc", obj.Name.Value);
            Assert.Equal("1.20", Assert.IsType<FloatValueNode>(obj.Value).Value);
        }

        [Fact]
        public void Create_Int()
        {
            // arrange
            // act
            var obj = new ObjectFieldNode("abc", 1);

            // assert
            Assert.Equal("abc", obj.Name.Value);
            Assert.Equal("1", Assert.IsType<IntValueNode>(obj.Value).Value);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void Create_Bool(bool value)
        {
            // arrange
            // act
            var obj = new ObjectFieldNode("abc", value);

            // assert
            Assert.Equal("abc", obj.Name.Value);
            Assert.Equal(value, Assert.IsType<BooleanValueNode>(obj.Value).Value);
        }

        [Fact]
        public void Create_String()
        {
            // arrange
            // act
            var obj = new ObjectFieldNode("abc", "def");

            // assert
            Assert.Equal("abc", obj.Name.Value);
            Assert.Equal("def", Assert.IsType<StringValueNode>(obj.Value).Value);
        }
    }
}
