using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class SchemaSerializerTests
    {
        [Fact]
        public void Serialize_SchemaIsNull_ArgumentNullException()
        {
            // arrange
            // act
            void Action() => SchemaSerializer.Serialize(null);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void SerializeSchemaWriter_SchemaIsNull_ArgumentNullException()
        {
            // arrange
            // act
            void Action() => SchemaSerializer.Serialize(null, new StringWriter());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void SerializeSchemaWriter_WriterIsNull_ArgumentNullException()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar")
                .Create();

            // act
            void Action() => SchemaSerializer.Serialize(schema, null);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void SerializeAsync_SchemaIsNull_ArgumentNullException()
        {
            // arrange
            // act
            async Task Action() => await SchemaSerializer.SerializeAsync(null, new MemoryStream());

            // assert
            Assert.ThrowsAsync<ArgumentNullException>(Action);
        }

        [Fact]
        public void SerializeAsync_WriterIsNull_ArgumentNullException()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar")
                .Create();

            // act
            async Task Action() => await SchemaSerializer.SerializeAsync(schema, null);

            // assert
            Assert.ThrowsAsync<ArgumentNullException>(Action);
        }

        [Fact]
        public void SerializeSchemaWriter_Serialize()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar")
                .Create();
            var stringBuilder = new StringBuilder();

            // act
            SchemaSerializer.Serialize(schema, new StringWriter(stringBuilder));

            // assert
            stringBuilder.ToString().MatchSnapshot();
        }

        [Fact]
        public void SerializeAsync_Serialize()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString("type Query { foo: String }")
                .AddResolver("Query", "foo", "bar")
                .Create();
            using var stream = new MemoryStream();

            // act
            SchemaSerializer.SerializeAsync(schema, stream);

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).MatchSnapshot();
        }

        [Fact]
        public void SerializeSchemaWithDirective()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(FileResource.Open("serialize_schema.graphql"))
                .AddResolver<Query>()
                .Use(next => next)
                .AddDirectiveType(new DirectiveType(t => t
                    .Name("upper")
                    .Location(DirectiveLocation.FieldDefinition)))
                .Create();

            // act
            var serializedSchema = schema.ToString();

            // assert
            serializedSchema.MatchSnapshot();
        }

        [Fact]
        public void SerializeSchemaWithMutationWithoutSubscription()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(FileResource.Open("serialize_schema_with_mutation.graphql"))
                .Use(next => next)
                .Create();

            // act
            var serializedSchema = schema.ToString();

            // assert
            serializedSchema.MatchSnapshot();
        }

        public class Query
        {
            public string Bar { get; set; }
        }
    }
}
