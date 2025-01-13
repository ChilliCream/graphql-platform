using HotChocolate.Types;
using HotChocolate.Tests;

namespace HotChocolate.Execution;

public class SchemaDirectiveTests
{
    [Fact]
    public void DirectivesOnObjectType()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result = schema.MakeExecutable().Execute("{ person { phone } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void DirectivesOnFieldDefinition()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result = schema.MakeExecutable().Execute("{ person { name } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void DirectivesOnFieldSelection()
    {
        // arrange
        var schema = CreateSchema();

        // act
        var result = schema.MakeExecutable().Execute("{ person { name @c(append:\"Baz\") } }");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void ExecDirectiveOrderIsSignificant()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddDocumentFromString("type Query { a: String }")
            .AddDirectiveType<UpperCaseDirectiveType>()
            .AddDirectiveType<LowerCaseDirectiveType>()
            .AddResolver("Query", "a", () => "hello")
            .Create();

        // act
        var result = schema.MakeExecutable().Execute("{ a @lower @upper b: a @upper @lower }");

        // assert
        result.MatchSnapshot();
    }

    public static ISchema CreateSchema()
        => SchemaBuilder.New()
            .AddDirectiveType<ResolveDirective>()
            .AddDirectiveType<BDirectiveType>()
            .AddDirectiveType<CDirectiveType>()
            .AddDirectiveType<UpperCaseDirectiveType>()
            .AddType<Query>()
            .AddType<PersonType>()
            .Create();

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

    public class PersonType : ObjectType<Person>
    {
        protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
        {
            descriptor.Directive(new Resolve());
            descriptor.Field(t => t.Name).Directive(new BDirective(append: "Bar"));
        }
    }

    public class BDirectiveType : DirectiveType<BDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<BDirective> descriptor)
        {
            descriptor.Name("b");
            descriptor.Location(DirectiveLocation.Interface);
            descriptor.Location(DirectiveLocation.FieldDefinition);
            descriptor.Use((next, directive) => async context =>
            {
                await next.Invoke(context);

                var s = directive.AsValue<BDirective>().Append;
                context.Result += s;
            });
        }
    }

    public class CDirectiveType : DirectiveType<CDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<CDirective> descriptor)
        {
            descriptor.Name("c");
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Use((next, directive) => async context =>
            {
                await next.Invoke(context);

                var s = directive.AsValue<CDirective>().Append;
                context.Result += s;
            });
        }
    }

    public class UpperCaseDirectiveType : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("upper");
            descriptor.Location(DirectiveLocation.Field | DirectiveLocation.FieldDefinition);
            descriptor.Use((next, directive) => async context =>
            {
                await next(context);

                if (directive.Type.Name != "upper")
                {
                    throw new GraphQLException("Not the upper directive.");
                }

                if (context.Result is string s)
                {
                    context.Result = s.ToUpperInvariant();
                }
            });
        }
    }

    public class LowerCaseDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("lower");
            descriptor.Location(DirectiveLocation.Field
                | DirectiveLocation.FieldDefinition);
            descriptor.Use((next, directive) => async context =>
            {
                await next(context);

                if (directive.Type.Name != "lower")
                {
                    throw new GraphQLException("Not the lower directive.");
                }

                if (context.Result is string s)
                {
                    context.Result = s.ToLowerInvariant();
                }
            });
        }
    }

    public class ADirective(string append)
    {
        public string Append { get; set; } = append;
    }

    public class BDirective(string append) : ADirective(append)
    {
    }

    public class CDirective(string append) : ADirective(append)
    {
    }

    public sealed class ResolveDirective : DirectiveType<Resolve>
    {
        protected override void Configure(IDirectiveTypeDescriptor<Resolve> descriptor)
        {
            descriptor.Name("resolve");

            descriptor.Use((next, _) => async context =>
            {
                context.Result = await context.ResolveAsync<object>().ConfigureAwait(false);
                await next.Invoke(context).ConfigureAwait(false);
            });

            descriptor
                .Location(DirectiveLocation.Schema)
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.Interface)
                .Location(DirectiveLocation.FieldDefinition)
                .Location(DirectiveLocation.Field);
        }
    }

    public sealed class Resolve;
}
