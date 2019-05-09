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
            Action action = () => SchemaSerializer.Serialize(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void SerializeSchemaWriter_SchemaIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaSerializer.Serialize(
                null, new StringWriter());

            // assert
            Assert.Throws<ArgumentNullException>(action);
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
            Action action = () => SchemaSerializer.Serialize(schema, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
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
            var stringBuiler = new StringBuilder();

            // act
            SchemaSerializer.Serialize(
                schema, new StringWriter(stringBuiler));

            // assert
            stringBuiler.ToString().MatchSnapshot();
        }

        [Fact]
        public void SerializeSchemaWithDirective()
        {
            // arrange
            string source = FileResource.Open("serialize_schema.graphql");
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
            string serializedSchema = schema.ToString();

            // assert
            serializedSchema.MatchSnapshot();
        }

        [Fact]
        public void SerializeSchemaWithMutationWithoutSubscription()
        {
            // arrange
            string source = FileResource.Open(
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
            string serializedSchema = schema.ToString();

            // assert
            serializedSchema.MatchSnapshot();
        }

        public class Query
        {
            public string Bar { get; set; }
        }
    }
}
