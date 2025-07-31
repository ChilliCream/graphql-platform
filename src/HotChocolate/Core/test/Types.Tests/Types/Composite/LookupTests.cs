#nullable enable

using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class LookupTests
{
    [Fact]
    public static async Task Lookup()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query1>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query1
            }

            type Book1 {
              id: Int!
              title: String!
            }

            type Query1 {
              bookById(id: Int!): Book1! @lookup
            }

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION
            """");
    }

    [Fact]
    public static async Task Lookup_With_Is_Argument()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query2>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query2
            }

            type Book2 {
              id: Int!
              title: String!
            }

            type Query2 {
              bookById(id: Int! @is(field: "id")): Book2! @lookup
            }

            """
            The @is directive is utilized on lookup fields to describe how the arguments
            can be mapped from the entity type that the lookup field resolves.
            """
            directive @is("The field selection map syntax." field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION

            scalar FieldSelectionMap
            """");
    }

    [Fact]
    public static async Task Entity_With_Key()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query3>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query3
            }

            type Book3 @key(fields: "id") {
              id: Int!
              title: String!
            }

            type Query3 {
              bookById(id: Int! @is(field: "id")): Book3! @lookup
            }

            """
            The @is directive is utilized on lookup fields to describe how the arguments
            can be mapped from the entity type that the lookup field resolves.
            """
            directive @is("The field selection map syntax." field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            """
            The @key directive is used to designate an entity's unique key,
            which identifies how to uniquely reference an instance of
            an entity across different source schemas.
            """
            directive @key("The field selection set syntax." fields: FieldSelectionSet!) on OBJECT | INTERFACE

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION

            scalar FieldSelectionMap

            scalar FieldSelectionSet
            """");
    }

    [Fact]
    public static async Task Lookup_With_OneOf()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query4>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query4
            }

            type Book4 @key(fields: "id") {
              id: Int!
              title: String!
            }

            type Query4 {
              book(by: Book4ByInput! @is(field: "{\n  id\n} | {\n  title\n}")): Book4! @lookup
            }

            input Book4ByInput @oneOf {
              id: Int
              title: String
            }

            """
            The @is directive is utilized on lookup fields to describe how the arguments
            can be mapped from the entity type that the lookup field resolves.
            """
            directive @is("The field selection map syntax." field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            """
            The @key directive is used to designate an entity's unique key,
            which identifies how to uniquely reference an instance of
            an entity across different source schemas.
            """
            directive @key("The field selection set syntax." fields: FieldSelectionSet!) on OBJECT | INTERFACE

            """
            The @lookup directive is used within a source schema to specify output fields
            that can be used by the distributed GraphQL executor to resolve an entity by
            a stable key.
            """
            directive @lookup on FIELD_DEFINITION

            "The `@oneOf` directive is used within the type system definition language to indicate that an input object is a oneof input object."
            directive @oneOf on INPUT_OBJECT

            scalar FieldSelectionMap

            scalar FieldSelectionSet
            """");
    }

    [Fact]
    public static async Task Require_With_Explicit_Field()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query5>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """"
            schema {
              query: Query5
            }

            type Book5 {
              someField(author: String! @require(field: "author")): String!
              id: Int!
              title: String!
            }

            type Query5 {
              book: Book5!
            }

            """
            The @require directive is used to express data requirements with other source schemas.
            Arguments annotated with the @require directive are removed from the composite schema
            and the value for these will be resolved by the distributed executor.


            directive @require(field: FieldSelectionMap!) on ARGUMENT_DEFINITION
            """
            directive @require("The field selection map syntax." field: FieldSelectionMap!) on ARGUMENT_DEFINITION

            scalar FieldSelectionMap
            """");
    }

    public class Query1
    {
        [Lookup]
        public Book1 GetBookById(int id)
            => new(id, "Abc");
    }

    public record Book1(int Id, string Title);

    public class Query2
    {
        [Lookup]
        public Book2 GetBookById([Is("id")] int id)
            => new(id, "Abc");
    }

    public record Book2(int Id, string Title);

    public class Query3
    {
        [Lookup]
        public Book3 GetBookById([Is("id")] int id)
            => new(id, "Abc");
    }

    [Key("id")]
    public record Book3(int Id, string Title);

    public class Query4
    {
        [Lookup]
        public Book4 GetBook([Is("{ id } | { title }")] Book4ByInput by)
            => new(by.Id ?? 1, by.Title ?? "Abc");
    }

    [OneOf]
    public record Book4ByInput(int? Id, string? Title);

    [Key("id")]
    public record Book4(int Id, string Title);

    public class Query5
    {
        public Book5 Book => new(1, "Abc");
    }

    public record Book5(int Id, string Title)
    {
        public string SomeField([Require("author")] string author)
            => author;
    }
}
