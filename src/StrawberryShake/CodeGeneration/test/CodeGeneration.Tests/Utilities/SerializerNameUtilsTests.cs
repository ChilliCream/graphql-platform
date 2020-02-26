using System.Threading.Tasks;
using HotChocolate;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public class SerializerNameUtilsTests
    {
        [Fact]
        public void Deserialize_List()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        list: [String]
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            string deserializerName =
                SerializerNameUtils.CreateDeserializerName(schema.QueryType.Fields["list"].Type);

            Assert.Equal("DeserializeNullableListOfNullableString", deserializerName);
        }

        [Fact]
        public void Deserialize_NonNull_List()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        nonNullList: [String]!
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            var deserializerName =
                SerializerNameUtils.CreateDeserializerName(
                    schema.QueryType.Fields["nonNullList"].Type);

            Assert.Equal("DeserializeListOfNullableString", deserializerName);
        }

        [Fact]
        public void Deserialize_NonNull_List_With_NonNull_Element()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        nonNullListNonNullElement: [String!]!
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            var deserializerName =
                SerializerNameUtils.CreateDeserializerName(
                    schema.QueryType.Fields["nonNullListNonNullElement"].Type);

            Assert.Equal("DeserializeListOfString", deserializerName);
        }

        [Fact]
        public void Deserialize_List_With_NonNull_Element()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        listNonNullElement: [String!]
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            var deserializerName =
                SerializerNameUtils.CreateDeserializerName(
                    schema.QueryType.Fields["listNonNullElement"].Type);

            Assert.Equal("DeserializeNullableListOfString", deserializerName);
        }

        [Fact]
        public void Deserialize_String()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        field: String
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            var deserializerName =
                SerializerNameUtils.CreateDeserializerName(
                    schema.QueryType.Fields["field"].Type);

            Assert.Equal("DeserializeNullableString", deserializerName);
        }

        [Fact]
        public void Deserialize_NonNull_String()
        {
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(@"
                    type Query {
                        nonNullField: String!
                    }
                ")
                .Use(next => context => Task.CompletedTask)
                .Create();

            var deserializerName =
                SerializerNameUtils.CreateDeserializerName(
                    schema.QueryType.Fields["nonNullField"].Type);

            Assert.Equal("DeserializeString", deserializerName);
        }
    }
}
