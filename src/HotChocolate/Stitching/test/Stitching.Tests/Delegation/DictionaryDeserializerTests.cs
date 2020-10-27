using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Stitching.Delegation
{
    public class DictionaryDeserializerTests
    {
        [Fact]
        public async Task Deserialize_NullValueNode()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType person = schema.GetType<ObjectType>("Person");

            // act
            object value = DictionaryDeserializer.DeserializeResult(person, NullValueNode.Default);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public async Task Deserialize_Null()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType person = schema.GetType<ObjectType>("Person");

            // act
            object value = DictionaryDeserializer.DeserializeResult(person, null);

            // assert
            Assert.Null(value);
        }

        [Fact]
        public async Task Deserialize_Dictionary()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType person = schema.GetType<ObjectType>("Person");

            var dict = new Dictionary<string, object>();

            // act
            object value = DictionaryDeserializer.DeserializeResult(person, dict);

            // assert
            Assert.Same(dict, value);
        }

        [Fact]
        public async Task Deserialize_String()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType stringType = schema.GetType<StringType>("String");

            // act
            object value = DictionaryDeserializer.DeserializeResult(stringType, "abc");

            // assert
            Assert.Equal("abc", value);
        }

        [Fact]
        public async Task Deserialize_StringValueNode()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType stringType = schema.GetType<StringType>("String");

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                stringType, new StringValueNode("abc"));

            // assert
            Assert.Equal("abc", value);
        }

        [Fact]
        public async Task Deserialize_StringList_StringList()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType stringListType = new ListType(schema.GetType<StringType>("String"));

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                stringListType, new List<string> { "abc" });

            // assert
            Assert.Collection(
                ((IEnumerable<string>)value)!,
                v => Assert.Equal("abc", v));
        }

        [Fact]
        public async Task Deserialize_StringList_ListOfStringValueNode()
        {
            // arrange
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQLServer()
                    .AddQueryType<Query>()
                    .BuildSchemaAsync();

            IType stringListType = new ListType(schema.GetType<StringType>("String"));

            // act
            object value = DictionaryDeserializer.DeserializeResult(
                stringListType,
                new List<object> { new StringValueNode("abc") });

            // assert
            Assert.Collection(
                ((IEnumerable<string>)value)!,
                v => Assert.Equal("abc", v));
        }

        public class Query
        {
            public Person GetPerson() => new Person();
        }

        public class Person
        {
            public string Name { get; } = "Jon Doe";
        }
    }
}
