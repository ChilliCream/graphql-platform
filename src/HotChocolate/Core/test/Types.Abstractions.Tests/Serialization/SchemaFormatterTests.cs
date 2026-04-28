using System.Text;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Serialization;

public class SchemaFormatterTests
{
    [Fact]
    public void Format_Single_InputObject_Type()
    {
        // arrange
        const string sdl =
            """
            input Foo {
                field: String
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            input Foo {
              field: String
            }
            """);
    }

    [Fact]
    public void Format_Single_InputObject_Type_Spec_Scalars_Do_Not_Need_To_Be_Declared()
    {
        // arrange
        const string sdl =
            """
            input Foo {
                field: String
            }
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            input Foo {
              field: String
            }
            """);
    }

    [Fact]
    public void Format_Two_InputObject_Extensions_Into_One()
    {
        // arrange
        const string sdl =
            """
            extend input Foo {
                field1: String
            }

            extend input Foo {
                field2: [String]!
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            extend input Foo {
              field1: String
              field2: [String]!
            }
            """);
    }

    [Fact]
    public void Format_Single_Object_Type()
    {
        // arrange
        const string sdl =
            """
            type Foo {
                field: String
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            type Foo {
              field: String
            }
            """);
    }

    [Fact]
    public void Format_Two_Object_Extensions_Into_One()
    {
        // arrange
        const string sdl =
            """
            extend type Foo {
                field1: String
            }

            extend type Foo {
                field2: [String]!
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            extend type Foo {
              field1: String
              field2: [String]!
            }
            """);
    }

    [Fact]
    public void Format_Single_Interface_Type()
    {
        // arrange
        const string sdl =
            """
            interface Foo {
                field: String
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            interface Foo {
              field: String
            }
            """);
    }

    [Fact]
    public void Format_Two_interface_Extensions_Into_One()
    {
        // arrange
        const string sdl =
            """
            extend interface Foo {
                field1: String
            }

            extend interface Foo {
                field2: [String]!
            }

            scalar String
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            extend interface Foo {
              field1: String
              field2: [String]!
            }
            """);
    }

    [Fact]
    public void Format_Directive_Type()
    {
        // arrange
        const string sdl =
            """
            directive @foo on FIELD_DEFINITION
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            directive @foo on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Format_Directive_Type_With_Arguments()
    {
        // arrange
        const string sdl =
            """
            directive @foo(a: String! b: [Foo] c: [Int!]) on FIELD_DEFINITION

            input Foo {
                a: Boolean
            }
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            input Foo {
              a: Boolean
            }

            directive @foo(a: String!, b: [Foo], c: [Int!]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Format_Natural_Order()
    {
        // arrange
        const string sdl =
            """
            directive @foo(b: [Foo] c: [Int!] a: String!) on FIELD_DEFINITION

            input Foo {
                a: Boolean
            }

            input Bar {
                a: Boolean
            }
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(
            schema,
            new SchemaFormatterOptions { OrderTypesByName = false, OrderFieldsByName = false });

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            directive @foo(b: [Foo], c: [Int!], a: String!) on FIELD_DEFINITION

            input Foo {
              a: Boolean
            }

            input Bar {
              a: Boolean
            }
            """);
    }

    [Fact]
    public void Format_Schema_With_Description_Schema_Keyword_Not_Omitted()
    {
        // arrange
        const string sdl =
            """
            "Example schema"
            schema {
                query: Query
                mutation: Mutation
            }

            type Query {
                someField: String
            }

            type Mutation {
                someMutation: String
            }
            """;

        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(schema);

        // assert
        formattedSdl.MatchInlineSnapshot(sdl);
    }

    [Fact]
    public void Format_IncludeInternalDirectives_True_Emits_Internal_Directive_Definition()
    {
        // arrange
        const string sdl =
            """
            type Query {
              foo: String
            }

            directive @internalDir on FIELD_DEFINITION
            """;
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        schema.DirectiveDefinitions["internalDir"].IsPublic = false;

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(
            schema,
            new SchemaFormatterOptions { IncludeInternalDirectives = true });

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              foo: String
            }

            directive @internalDir on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Format_IncludeInternalDirectives_False_Strips_Internal_Directive_Definition()
    {
        // arrange
        const string sdl =
            """
            type Query {
              foo: String
            }

            directive @internalDir on FIELD_DEFINITION
            """;
        var schema = SchemaParser.Parse(Encoding.UTF8.GetBytes(sdl));
        schema.DirectiveDefinitions["internalDir"].IsPublic = false;

        // act
        var formattedSdl = SchemaFormatter.FormatAsString(
            schema,
            new SchemaFormatterOptions { IncludeInternalDirectives = false });

        // assert
        formattedSdl.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              foo: String
            }
            """);
    }
}
