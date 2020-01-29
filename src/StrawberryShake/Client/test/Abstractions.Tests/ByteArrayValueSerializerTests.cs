using System.Text;
using System;
using StrawberryShake.Serializers;
using Xunit;

namespace StrawberryShake
{
    public class ByteArrayValueSerializerTests
    {

        [Fact]
        public void Kind_isString()
        {
            // arrange
            var serializer = new ByteArrayValueSerializer();

            // assert
            Assert.Equal(ValueKind.String, serializer.Kind);
        }

        [Fact]
        public void Serialize_Null()
        {
            // arrange
            var serializer = new ByteArrayValueSerializer();

            // act
            var result = serializer.Serialize(null);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void Serialize_ByteArray()
        {
            // arrange
            var serializer = new ByteArrayValueSerializer();
            byte[] bytes = Encoding.ASCII.GetBytes("data");
            string base64 = Convert.ToBase64String(bytes);
            // act
            var result = serializer.Serialize(bytes);

            // assert
            Assert.Equal(base64, result);
        }

        [Fact]
        public void Serialize_InvalidType_ThrowsArgumentException()
        {
            // arrange
            var serializer = new ByteArrayValueSerializer();

            // act
            Action a = () => serializer.Serialize(5);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void Deserialize_Null()
        {
            // arrange
            var serializer = new ByteArrayValueSerializer();

            // act
            var result = serializer.Deserialize(null);

            // assert
            Assert.Null(result);
        }

        [Fact]
        public void Deserialize_Base64()
        {
            // arrange
            var serializer = new ByteArrayValueSerializer();
            byte[] bytes = Encoding.ASCII.GetBytes("data");
            string base64 = Convert.ToBase64String(bytes);
            // act
            var result = serializer.Deserialize(base64);

            // assert
            Assert.Equal(bytes, result);
        }

        [Fact]
        public void Deserialize_InvalidType_ThrowsArgumentException()
        {
            // arrange
            var serializer = new ByteArrayValueSerializer();

            // act
            Action a = () => serializer.Deserialize(5);

            // assert
            Assert.Throws<ArgumentException>(a);
        }
    }
}
