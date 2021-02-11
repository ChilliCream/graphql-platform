using Xunit;

namespace StrawberryShake.Serialization
{
    public class ByteArraySerializerTests
    {
        public ByteArraySerializer Serializer { get; } = new();

        public ByteArraySerializer CustomSerializer { get; } = new("Abc");

        [Fact]
        public void Parse()
        {
            // arrange
            byte[] buffer = { 1 };

            // act
            byte[] result = Serializer.Parse(buffer);

            // assert
            Assert.Same(buffer, result);
        }

        [Fact]
        public void Format_Null()
        {
            // arrange

            // act
            object? result = Serializer.Format(null);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void Format_Value()
        {
            // arrange
            byte[] buffer = { 1 };

            // act
            object? result = Serializer.Format(buffer);

            // assert
            Assert.Same(buffer, result);
        }

        [Fact]
        public void TypeName_Default()
        {
            // arrange

            // act
            string typeName = Serializer.TypeName;

            // assert
            Assert.Equal("ByteArray", typeName);
        }

        [Fact]
        public void TypeName_Custom()
        {
            // arrange

            // act
            string typeName = CustomSerializer.TypeName;

            // assert
            Assert.Equal("Abc", typeName);
        }
    }
}
