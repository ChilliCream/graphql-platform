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
        public void Foo()
        {
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


            SchemaSerializer.Serialize(schema, s);

            sb.ToString().Snapshot();



        }

        public class Baz
        {
            public string Name { get; set; }
        }




    }
}
