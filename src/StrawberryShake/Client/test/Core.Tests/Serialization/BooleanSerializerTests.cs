using Xunit;

namespace StrawberryShake.Serialization
{
    public class BooleanSerializerTests
    {
        [Fact]
        public void Parse()
        {
            // arrange
            var serializer = new BooleanSerializer();

            // act
            bool? result = serializer.Parse(true);

            // assert
            Assert.True(Assert.IsType<bool>(result));
        }

        [Fact]
        public void Format_Null()
        {
            // arrange
            var serializer = new BooleanSerializer();

            // act
            object? result = serializer.Format(null);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void Format_True()
        {
            // arrange
            var serializer = new BooleanSerializer();

            // act
            object? result = serializer.Format(true);

            // assert
            Assert.True(Assert.IsType<bool>(result));
        }

        [Fact]
        public void Format_False()
        {
            // arrange
            var serializer = new BooleanSerializer();

            // act
            object? result = serializer.Format(false);

            // assert
            Assert.False(Assert.IsType<bool>(result));
        }

        [Fact]
        public void TypeName_Default()
        {
            // arrange
            var serializer = new BooleanSerializer();

            // act
            string typeName = serializer.TypeName;

            // assert
            Assert.Equal("Boolean", typeName);
        }

        [Fact]
        public void TypeName_Custom()
        {
            // arrange
            var serializer = new BooleanSerializer("Abc");

            // act
            string typeName = serializer.TypeName;

            // assert
            Assert.Equal("Abc", typeName);
        }
    }
}
