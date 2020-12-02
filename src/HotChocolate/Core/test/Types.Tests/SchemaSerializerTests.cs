using System;
using System.IO;
using System.Text;
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
                .AddDocumentFromString(
                    "type Query { foo: String }")
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
            void Action() => SchemaSerializer.SerializeAsync(null, new MemoryStream());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void SerializeAsync_WriterIsNull_ArgumentNullException()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    "type Query { foo: String }")
                .AddResolver("Query", "foo", "bar")
                .Create();

            // act
            void Action() => SchemaSerializer.SerializeAsync(schema, null);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void SerializeSchemaWriter_Serialize()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    "type Query { foo: String }")
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
                .AddDocumentFromString(
                    "type Query { foo: String }")
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
            var source = FileResource.Open("serialize_schema.graphql");
            ISchema schema = Schema.Create(
                source,
                c =>
                {
                    c.RegisterQueryType<Query>();
                    c.Use(next => context => next(context));
                    c.RegisterDirective(new DirectiveType(t =>
                        t.Name("upper")
                            .Location(DirectiveLocation.FieldDefinition)));
                });


            // act
            var serializedSchema = schema.ToString();

            // assert
            serializedSchema.MatchSnapshot();
        }

        [Fact]
        public void SerializeSchemaWithMutationWithoutSubscription()
        {
            // arrange
            var source = FileResource.Open(
                "serialize_schema_with_mutation.graphql");
            ISchema schema = Schema.Create(
                source,
                c =>
                {
                    c.Use(next => context => next(context));
                });

            var sb = new StringBuilder();
            var s = new StringWriter(sb);

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
