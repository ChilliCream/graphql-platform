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
