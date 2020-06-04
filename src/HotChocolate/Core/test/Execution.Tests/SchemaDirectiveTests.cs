using System.Threading.Tasks;
using HotChocolate.Types;
using Snapshooter.Xunit;
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
                schema.MakeExecutable().Execute("{ person { phone } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void DirectivesOnFieldDefinition()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.MakeExecutable().Execute("{ person { name } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void DirectivesOnFieldSelection()
        {
            // arrange
            ISchema schema = CreateSchema();

            // act
            IExecutionResult result =
                schema.MakeExecutable().Execute(
                    "{ person { name @c(append:\"Baz\") } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void ExecDirectiveOrderIsSignificant()
        {
            // arrange
            ISchema schema = Schema.Create(
                "type Query { a: String }",
                c =>
                {
                    c.RegisterDirective<UpperCaseDirectiveType>();
                    c.RegisterDirective<LowerCaseDirectiveType>();
                    c.BindResolver(() => "hello").To("Query", "a");
                });

            // act
            IExecutionResult result =
                schema.MakeExecutable().Execute(
                    "{ a @lower @upper b: a @upper @lower }");

            // assert
            result.MatchSnapshot();
        }

        public static ISchema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterDirective<ResolveDirective>();
                c.RegisterDirective<ADirectiveType>();
                c.RegisterDirective<BDirectiveType>();
                c.RegisterDirective<CDirectiveType>();
                c.RegisterDirective<UpperCaseDirectiveType>();
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
                descriptor.Use(next => context =>
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
                descriptor.Use(next => context =>
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
                descriptor.Use(next => context =>
                {
                    string s = context.Directive
                        .ToObject<CDirective>()
                        .Append;
                    context.Result = context.Result + s;
                    return default(ValueTask);
                });
            }
        }

        public class UpperCaseDirectiveType
           : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("upper");
                descriptor.Location(DirectiveLocation.Field
                    | DirectiveLocation.FieldDefinition);
                descriptor.Use(next => async context =>
                {
                    await next(context);

                    if (context.Directive.Name != "upper")
                    {
                        throw new QueryException("Not the upper directive.");
                    }

                    if (context.Result is string s)
                    {
                        context.Result = s.ToUpperInvariant();
                    }
                });
            }
        }

        public class LowerCaseDirectiveType
           : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("lower");
                descriptor.Location(DirectiveLocation.Field
                    | DirectiveLocation.FieldDefinition);
                descriptor.Use(next => async context =>
                {
                    await next(context);

                    if (context.Directive.Name != "lower")
                    {
                        throw new QueryException("Not the lower directive.");
                    }

                    if (context.Result is string s)
                    {
                        context.Result = s.ToLowerInvariant();
                    }
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

        public sealed class ResolveDirective
            : DirectiveType<Resolve>
        {
            protected override void Configure(IDirectiveTypeDescriptor<Resolve> descriptor)
            {
                descriptor.Name("resolve");
                descriptor.Use(next => async context =>
                {
                    context.Result = await context.ResolveAsync<object>()
                        .ConfigureAwait(false);

                    await next.Invoke(context).ConfigureAwait(false);
                });

                descriptor.Location(DirectiveLocation.Schema)
                    .Location(DirectiveLocation.Object)
                    .Location(DirectiveLocation.Interface)
                    .Location(DirectiveLocation.FieldDefinition)
                    .Location(DirectiveLocation.Field);
            }
        }

        public sealed class Resolve { }
    }
}
