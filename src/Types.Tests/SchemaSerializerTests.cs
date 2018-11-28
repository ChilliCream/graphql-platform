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
                    c.BindType<Baz>().To("BazInput");
                });

            var sb = new StringBuilder();
            var s = new StringWriter(sb);

            // act
            SchemaSerializer.Serialize(schema, s);

            // assert
            sb.ToString().Snapshot();
        }

        public class Baz
        {
            public string Name { get; set; }
        }
    }
}
