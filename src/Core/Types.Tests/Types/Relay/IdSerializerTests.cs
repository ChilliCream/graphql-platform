using System;
using System.Text;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class IdSerializerTests
    {
        [Fact]
        public void Serialize_TypeNameIsEmpty_ArgumentException()
        {
            // arrange
            var serializer = new IdSerializer();

            // act
            Action a = () => serializer.Serialize("", 123);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void Serialize_IdIsNull_ArgumentNullException()
        {
            // arrange
            var serializer = new IdSerializer();

            // act
            Action a = () => serializer.Serialize("Foo", default(object));

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void Deserialize_SerializedIsNull_ArgumentNullException()
        {
            // arrange
            var serializer = new IdSerializer();

            // act
            Action a = () => serializer.Deserialize(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void IsPossibleBase64String_sIsNull_ArgumentNullException()
        {
            // arrange
            var serializer = new IdSerializer();

            // act
            Action a = () => IdSerializer.IsPossibleBase64String(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [InlineData("123", "\0Bar\nFoo\nd123")]
        [Theory]
        public void SerializeIdValueWithSchemaName(object id, string expected)
        {
            // arrange
            NameString schema = "Bar";
            NameString typeName = "Foo";
            var serializer = new IdSerializer();

            // act
            string serializedId = serializer.Serialize(schema, typeName, id);

            // assert
            string unwrapped = Encoding.UTF8.GetString(
                Convert.FromBase64String(serializedId));
            Assert.Equal(expected, unwrapped);
        }

        [InlineData("AEJhcgpGb28KZDEyMw==", "123", typeof(string))]
        [Theory]
        public void DeserializeIdValueWithSchemaName(
            string serialized, object id, Type idType)
        {
            // arrange
            var serializer = new IdSerializer();

            // act
            IdValue value = serializer.Deserialize(serialized);

            // assert
            Assert.IsType(idType, value.Value);
            Assert.Equal(id, value.Value);
            Assert.Equal("Foo", value.TypeName);
            Assert.Equal("Bar", value.SchemaName);
        }


        [InlineData((short)123, "Foo\ns{\0\0")]
        [InlineData(123, "Foo\ni{\0\0\0\0")]
        [InlineData((long)123, "Foo\nl{\0\0\0\0\0\0\0\0")]
        [InlineData("123456", "Foo\nd123456")]
        [Theory]
        public void SerializeIdValue(object id, string expected)
        {
            // arrange
            NameString typeName = "Foo";
            var serializer = new IdSerializer();

            // act
            string serializedId = serializer.Serialize(typeName, id);

            // assert
            string unwrapped = Encoding.UTF8.GetString(
                Convert.FromBase64String(serializedId));
            Assert.Equal(expected, unwrapped);
        }

        [Fact]
        public void SerializeGuidValue()
        {
            // arrange
            NameString typeName = "Foo";
            var id = new Guid("dad4f33d303345d7b7541d9ac23974d9");
            var serializer = new IdSerializer();

            // act
            string serializedId = serializer.Serialize(typeName, id);

            // assert
            string unwrapped = Encoding.UTF8.GetString(
                Convert.FromBase64String(serializedId));
            Assert.StartsWith("Foo\ng=", unwrapped);
        }

        [InlineData("Rm9vCnN7AAA=", (short)123, typeof(short))]
        [InlineData("Rm9vCml7AAAAAA==", 123, typeof(int))]
        [InlineData("Rm9vCmx7AAAAAAAAAAA=", (long)123, typeof(long))]
        [InlineData("Rm9vCmQxMjM0NTY=", "123456", typeof(string))]
        [Theory]
        public void DeserializeIdValue(
            string serialized, object id, Type idType)
        {
            // arrange
            var serializer = new IdSerializer();

            // act
            IdValue value = serializer.Deserialize(serialized);

            // assert
            Assert.IsType(idType, value.Value);
            Assert.Equal(id, value.Value);
            Assert.Equal("Foo", value.TypeName);
        }

        [Fact]
        public void DeserializeGuidValue()
        {
            // arrange
            var serialized = "Rm9vCmc989TaMzDXRbdUHZrCOXTZAA==";
            var serializer = new IdSerializer();

            // act
            IdValue value = serializer.Deserialize(serialized);

            // assert
            Assert.Equal(
                new Guid("dad4f33d303345d7b7541d9ac23974d9"),
                Assert.IsType<Guid>(value.Value));
            Assert.Equal("Foo", value.TypeName);
        }

        [InlineData("Rm9vLXN7===", false)]
        [InlineData("Rm9vLXN7====", false)]
        [InlineData("====", false)]
        [InlineData("=Rm9=vLXN7AA", false)]
        [InlineData("Rm9=vLXN7AA=", false)]
        [InlineData("Rm9vLXN7AA==", true)]
        [InlineData("Rm9vLWc989TaMzDXRbdUHZrCOXT", false)]
        [InlineData("Rm9vLWc989TaMzDXRbdUHZrCOXTZ", true)]
        [Theory]
        public void IsPossibleBase64String(string serialized, bool valid)
        {
            // arrange
            var serializer = new IdSerializer();

            // act
            bool result = IdSerializer.IsPossibleBase64String(serialized);

            // assert
            Assert.Equal(valid, result);
        }
    }
}
