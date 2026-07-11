using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaParserTests
{
    [Fact]
    public void Parse_SourceSchemaInvalidGraphQL_ReturnsError()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText("A", "type Query {");
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
        Assert.Equal("Source schema parsing failed.", result.Errors[0].Message);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: Expected a `Name`-token, "
            + "but found a `EndOfFile`-token..",
            entry.Message);
        Assert.Equal("A", entry.Schema?.Name);
    }

    [Fact]
    public void Parse_SourceSchemaWithSchemaName_SetsSchemaName()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText("A", "schema { }");
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        Assert.Equal("A", result.Value.Name);
    }

    [Fact]
    public void Parse_FederationRequiresWithoutDefinition_IsRecognized()
    {
        // arrange
        // a federation subgraph that applies @requires without declaring the directive
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                extend schema
                    @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@requires"])

                type Query {
                    product: Product
                }

                type Product @key(fields: "id") {
                    id: ID!
                    price: Float @external
                    shippingEstimate: Float @requires(fields: "price")
                }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        var requiresDefinition = result.Value.DirectiveDefinitions["requires"];
        Assert.IsNotType<MissingDirectiveDefinition>(requiresDefinition);
    }

    [Fact]
    public void Parse_NonFederationRequires_ReportsUndefinedDirective()
    {
        // arrange
        // a non-federation source schema that applies @requires must not have it recognized
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    product: Product
                }

                type Product {
                    id: ID!
                    price: Float
                    shippingEstimate: Float @requires(fields: "price")
                }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal("HCV0026", entry.Code);
    }

    [Fact]
    public void Parse_OutputFieldUsesInputObjectType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    exportFoos: [FooInput!]!
                }

                type Bar {
                    id: ID!
                }

                input FooInput {
                    id: ID!
                }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The field 'Query.exportFoos' must return an output type..",
            entry.Message);
    }

    [Fact]
    public void Parse_OutputFieldUsesInputObjectTypeDeepInSchema_DoesNotCascade()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    exportFoos: [FooInput!]!
                    foo: Foo!
                }

                type Foo {
                    id: ID!
                    name: String!
                }

                type Bar {
                    id: ID!
                }

                type Baz {
                    id: ID!
                }

                input FooInput {
                    id: ID!
                }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The field 'Query.exportFoos' must return an output type..",
            entry.Message);
        Assert.DoesNotContain(log, e => e.Code == "HCV0001");
    }

    [Fact]
    public void Parse_InputObjectFieldUsesObjectType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo(input: FooInput!): String!
                }

                input FooInput {
                    foo: Foo!
                }

                type Foo {
                    id: ID!
                }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The Input Object field 'FooInput.foo' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_InputObjectFieldUsesInterfaceType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo(input: FooInput!): String!
                }

                input FooInput {
                    foo: [Foo!]!
                }

                interface Foo {
                    id: ID!
                }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The Input Object field 'FooInput.foo' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_InputObjectFieldUsesUnionType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo(input: FooInput!): String!
                }

                input FooInput {
                    foo: FooUnion!
                }

                union FooUnion = Foo | Bar

                type Foo { id: ID! }
                type Bar { id: ID! }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The Input Object field 'FooInput.foo' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_ArgumentUsesObjectType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo(arg: Foo): String!
                }

                type Foo {
                    id: ID!
                }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The argument 'Query.foo(arg:)' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_ArgumentUsesInterfaceType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo(arg: [Foo!]!): String!
                }

                interface Foo {
                    id: ID!
                }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The argument 'Query.foo(arg:)' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_ArgumentUsesUnionType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo(arg: FooUnion!): String!
                }

                union FooUnion = Foo | Bar

                type Foo { id: ID! }
                type Bar { id: ID! }
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The argument 'Query.foo(arg:)' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_DirectiveArgumentUsesObjectType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo: String!
                }

                type Foo {
                    id: ID!
                }

                directive @bar(arg: Foo!) on FIELD_DEFINITION
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The argument '@bar(arg:)' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_DirectiveArgumentUsesInterfaceType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo: String!
                }

                interface Foo {
                    id: ID!
                }

                directive @bar(arg: [Foo!]!) on FIELD_DEFINITION
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The argument '@bar(arg:)' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_DirectiveArgumentUsesUnionType_ReportsClearError()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    foo: String!
                }

                union FooUnion = Foo | Bar

                type Foo { id: ID! }
                type Bar { id: ID! }

                directive @baz(arg: FooUnion!) on FIELD_DEFINITION
                """);
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        var entry = Assert.Single(log);
        Assert.Equal(
            "Invalid GraphQL in source schema. Exception message: "
            + "The argument '@baz(arg:)' must accept an input type..",
            entry.Message);
    }

    [Fact]
    public void Parse_SourceSchemaWithSchemaErrors_ReturnsErrors()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText("A", "type Empty { }");
        var log = new CompositionLog();
        var parser = new SourceSchemaParser(sourceSchemaText, log);

        // act
        var result = parser.Parse();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal("Source schema parsing failed.", result.Errors[0].Message);
        var entry = Assert.Single(log);
        Assert.Equal(
            "The Object type 'Empty' must define one or more fields. (Schema: 'A')",
            entry.Message);
        Assert.Equal("HCV0001", entry.Code);
    }
}
