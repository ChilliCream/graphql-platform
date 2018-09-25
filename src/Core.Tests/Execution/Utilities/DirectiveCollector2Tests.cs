using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class DirectiveCollector2Tests
    {

        [Fact]
        public void Collect()
        {
            // arrange
            ISchema schema = CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    foo {
                        name @bar
                        bar {
                            name
                        }
                    }
                }
            ");

            // act
            var collector = new DirectiveCollector2(schema);
            collector.VisitDocument(query);

        }

        private ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterDirective<FooDirectiveType>();
                c.RegisterDirective<BarDirectiveType>();
                c.RegisterType<QueryType>();
                c.RegisterType<FooType>();
                c.RegisterType<BarType>();
            });
        }

        public class FooDirectiveType
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("foo");
                descriptor.OnInvokeResolver(
                    (ctx, dir, exec, ct) => Task.FromResult<object>("FooDir"));
                descriptor.Location(Types.DirectiveLocation.FieldDefinition);
                descriptor.Location(Types.DirectiveLocation.Object);
            }
        }

        public class BarDirectiveType
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("bar");
                descriptor.Location(Types.DirectiveLocation.FieldDefinition);
                descriptor.Location(Types.DirectiveLocation.Object);
                descriptor.Location(Types.DirectiveLocation.Field);
            }
        }


        public class QueryType
            : ObjectType<Query>
        {

        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Directive("foo");
            }
        }

        public class BarType
            : ObjectType<Bar>
        {

        }

        public class Query
        {
            public Foo Foo { get; set; } =
                new Foo
                {
                    Name = "Foo",
                    Bar = new Bar
                    {
                        Name = "Bar"
                    }
                };
        }

        public class Foo
        {
            public Bar Bar { get; set; }

            public string Name { get; set; }
        }

        public class Bar
        {
            public string Name { get; set; }
        }
    }
}
