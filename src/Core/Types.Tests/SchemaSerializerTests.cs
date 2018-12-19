using System.IO;
using System.Text;
using ChilliCream.Testing;
using HotChocolate.Types;
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
                    c.Use(next => context => next(context));
                    c.RegisterDirective(new DirectiveType(t =>
                        t.Name("upper")
                            .Location(DirectiveLocation.FieldDefinition)));
                });

            var sb = new StringBuilder();
            var s = new StringWriter(sb);

            // act
            SchemaSerializer.Serialize(schema, s);

            // assert
            sb.ToString().Snapshot();
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
            SchemaSerializer.Serialize(schema, s);

            // assert
            sb.ToString().Snapshot();
        }
    }
}
