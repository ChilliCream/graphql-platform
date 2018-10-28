using System.Threading.Tasks;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class SchemaDirectiveTests
    {
        [Fact]
        public void DirectivesOnObjectType()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.Execute("{ person { phone } }");

            // assert
            result.Snapshot();
        }

        [Fact]
        public void DirectivesOnFieldDefinition()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.Execute("{ person { name } }");

            // assert
            result.Snapshot();
        }

        [Fact]
        public void DirectivesOnFieldSelection()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.Execute("{ person { name @c(append:\"Baz\") } }");

            // assert
            result.Snapshot();
        }

        public static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterDirective<ResolveDirective>();
                c.RegisterDirective<ADirectiveType>();
                c.RegisterDirective<BDirectiveType>();
                c.RegisterDirective<CDirectiveType>();
                c.RegisterQueryType<Query>();
                c.RegisterType<PersonType>();
            });
        }

        public class Query
        {
            public Person GetPerson() => new Person();
        }

        public class Person
        {
            public string Name { get; set; } = "Name";
            public string Phone { get; set; } = "Phone";
            public string ZipCode { get; set; } = "ZipCode";
            public string Country { get; set; } = "Country";
        }

        public class PersonType
            : ObjectType<Person>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Person> descriptor)
            {
                descriptor.Directive(new Resolve());
                descriptor.Directive(new ADirective { Append = "Foo" });
                descriptor.Field(t => t.Name)
                    .Directive(new BDirective { Append = "Bar" });
            }
        }

        public class ADirectiveType
            : DirectiveType<ADirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<ADirective> descriptor)
            {
                descriptor.Name("a");
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.Interface);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Middleware(next => context =>
                {
                    string s = context.Directive
                        .ToObject<ADirective>()
                        .Append;
                    context.Result = context.Result + s;
                    return next.Invoke(context);
                });
            }
        }


        public class BDirectiveType
           : DirectiveType<BDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<BDirective> descriptor)
            {
                descriptor.Name("b");
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.Interface);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Middleware(next => context =>
                {
                    string s = context.Directive
                        .ToObject<BDirective>()
                        .Append;
                    context.Result = context.Result + s;
                    return next.Invoke(context);
                });
            }
        }

        public class CDirectiveType
           : DirectiveType<CDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<CDirective> descriptor)
            {
                descriptor.Name("c");
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Middleware(next => context =>
                {
                    string s = context.Directive
                        .ToObject<CDirective>()
                        .Append;
                    context.Result = context.Result + s;
                    return Task.CompletedTask;
                });
            }
        }

        public class ADirective
        {
            public string Append { get; set; }
        }

        public class BDirective
            : ADirective
        {
        }

        public class CDirective
            : ADirective
        {
        }
    }
}
