using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using static HotChocolate.Validation.TestHelper;

namespace HotChocolate.Validation;

public class IntrospectionRuleTests
{
    [Fact]
    public void IntrospectionNotAllowed_Schema_Field()
    {
        ExpectErrors(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule(),
            @"
                {
                    __schema
                }");
    }

    [Fact]
    public void IntrospectionNotAllowed_Schema_Field_Custom_MessageFactory()
    {
        ExpectErrors(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule(),
            @"
                {
                    __schema
                }",
            new KeyValuePair<string, object>[]
            {
                new(WellKnownContextData.IntrospectionMessage, new Func<string>(() => "Bar")),
            });
    }

    [Fact]
    public void IntrospectionNotAllowed_Schema_Field_Custom_Message()
    {
        ExpectErrors(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule(),
            @"
                {
                    __schema
                }",
            new KeyValuePair<string, object>[]
            {
                new(WellKnownContextData.IntrospectionMessage, "Baz"),
            });
    }

    [Fact]
    public void IntrospectionNotAllowed_Type_Field()
    {
        ExpectErrors(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule(),
            @"
                {
                    __type(name: ""foo"")
                }");
    }

    [Fact]
    public void IntrospectionAllowed_Typename_Field()
    {
        ExpectValid(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule(),
            @"
                {
                    __typename
                }");
    }

    [Fact]
    public void IntrospectionAllowed_Schema_Field()
    {
        ExpectValid(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule(),
            @"{
                __schema {
                    name
                }
            }",
            new KeyValuePair<string, object>[]
            {
                new(WellKnownContextData.IntrospectionAllowed, null),
            });
    }

    [Fact]
    public void IntrospectionAllowed_Type_Field()
    {
        ExpectValid(
            CreateSchema(),
            b => b.AddIntrospectionAllowedRule(),
            @"
                {
                    __type(name: ""foo"")
                }",
            new KeyValuePair<string, object>[]
            {
                new(WellKnownContextData.IntrospectionAllowed, null),
            });
    }


    private ISchema CreateSchema()
    {
        return SchemaBuilder.New()
            .AddDocumentFromString(FileResource.Open("CostSchema.graphql"))
            .Use(_ => _ => default)
            .Create();
    }
}
