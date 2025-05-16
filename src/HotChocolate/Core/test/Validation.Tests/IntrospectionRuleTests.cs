using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Validation.TestHelper;

namespace HotChocolate.Validation;

public class IntrospectionRuleTests
{
    [Fact]
    public void IntrospectionNotAllowed_Schema_Field()
    {
        ExpectErrors(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __schema
            }
            """);
    }

    [Fact]
    public void IntrospectionNotAllowed_Schema_Field_Custom_MessageFactory()
    {
        ExpectErrors(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __schema
            }
            """,
            [
                new(ExecutionContextData.IntrospectionMessage, new Func<string>(() => "Bar"))
            ]);
    }

    [Fact]
    public void IntrospectionNotAllowed_Schema_Field_Custom_Message()
    {
        ExpectErrors(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __schema
            }
            """,
            [
                new(ExecutionContextData.IntrospectionMessage, "Baz")
            ]);
    }

    [Fact]
    public void IntrospectionNotAllowed_Type_Field()
    {
        ExpectErrors(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __type(name: "foo")
            }
            """);
    }

    [Fact]
    public void IntrospectionAllowed_Typename_Field()
    {
        ExpectValid(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __typename
            }
            """);
    }

    [Fact]
    public void IntrospectionAllowed_Schema_Field()
    {
        ExpectValid(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __schema {
                    name
                }
            }
            """,
            [
                new(ExecutionContextData.IntrospectionAllowed, null)
            ]);
    }

    [Fact]
    public void IntrospectionAllowed_Type_Field()
    {
        ExpectValid(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __type(name: "foo")
            }
            """,
            [
                new(ExecutionContextData.IntrospectionAllowed, null)
            ]);
    }

    private static Schema CreateSchema()
        => SchemaBuilder.New()
            .AddDocumentFromString(
                FileResource.Open("IntrospectionSchema.graphql"))
            .Use(_ => _ => default)
            .Create();
}
