using HotChocolate.Execution;
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
            context => context.Features.Set(
                new IntrospectionRequestOverrides(
                    IsAllowed: false,
                    NotAllowedErrorMessage: "Baz")));
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
            context => context.Features.Set(new IntrospectionRequestOverrides(IsAllowed: true)));
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
            context => context.Features.Set(new IntrospectionRequestOverrides(IsAllowed: true)));
    }

    [Fact]
    public void IntrospectionNotAllowed_Search_Field()
    {
        ExpectErrors(
            CreateSchemaWithSemanticIntrospection(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __search(query: "foo") {
                    coordinate
                }
            }
            """);
    }

    [Fact]
    public void IntrospectionNotAllowed_Definitions_Field()
    {
        ExpectErrors(
            CreateSchemaWithSemanticIntrospection(),
            b => b.AddIntrospectionAllowedRule()
                .ModifyOptions(o => o.DisableIntrospection = true),
            """
            {
                __definitions(coordinates: ["Foo"]) {
                    ... on __Type {
                        name
                    }
                }
            }
            """);
    }

    private static Schema CreateSchema()
        => SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("IntrospectionSchema.graphql"))
            .ModifyOptions(o => o.EnableSemanticIntrospection = false)
            .Use(_ => _ => default)
            .Create();

    private static Schema CreateSchemaWithSemanticIntrospection()
        => SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("IntrospectionSchema.graphql"))
            .Use(_ => _ => default)
            .Create();
}
