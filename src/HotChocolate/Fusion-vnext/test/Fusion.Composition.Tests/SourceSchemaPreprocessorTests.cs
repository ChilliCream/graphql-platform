using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaPreprocessorTests
{
    [Fact]
    public void Node_Field_Should_Be_Turned_Into_A_Lookup()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            type Query {
              node(id: ID!): Node
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              other: String!
            }

            type Review implements Node {
              id: ID!
              title: String!
            }
            """);
        var preprocessor = new SourceSchemaPreprocessor(schema);

        // act
        var result = preprocessor.Process();

        // assert
        SchemaFormatter.FormatAsString(result).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @shareable {
              node(id: ID!): Node
                @lookup
            }

            type Product implements Node
              @key(fields: "id")
              @shareable {
              id: ID!
              other: String!
            }

            type Review implements Node
              @key(fields: "id")
              @shareable {
              id: ID!
              title: String!
            }

            interface Node
              @key(fields: "id") {
              id: ID!
            }

            scalar FieldSelectionSet

            "The @key directive is used to designate an entity’s unique key, which identifies how to uniquely reference an instance of an entity across different source schemas."
            directive @key("Represents a selection set syntax." fields: FieldSelectionSet!) repeatable on OBJECT | INTERFACE

            "The @lookup directive is used within a source schema to specify output fields that can be used by the distributed GraphQL executor to resolve an entity by a stable key."
            directive @lookup on FIELD_DEFINITION

            "The @shareable directive allows multiple source schemas to define the same field, ensuring that this decision is both intentional and coordinated by requiring fields to be explicitly marked."
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            """);
    }

    [Fact]
    public void ById_Field_Should_Be_Turned_Into_Lookup()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            type Query {
              productById(id: ID!): Product
            }

            type Product {
              id: ID!
              other: String
            }
            """);
        var preprocessor = new SourceSchemaPreprocessor(schema);

        // act
        var result = preprocessor.Process();

        // assert
        SchemaFormatter.FormatAsString(result).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @shareable {
              productById(id: ID!): Product
                @lookup
            }

            type Product
              @key(fields: "id")
              @shareable {
              id: ID!
              other: String
            }

            scalar FieldSelectionSet

            "The @key directive is used to designate an entity’s unique key, which identifies how to uniquely reference an instance of an entity across different source schemas."
            directive @key("Represents a selection set syntax." fields: FieldSelectionSet!) repeatable on OBJECT | INTERFACE

            "The @lookup directive is used within a source schema to specify output fields that can be used by the distributed GraphQL executor to resolve an entity by a stable key."
            directive @lookup on FIELD_DEFINITION

            "The @shareable directive allows multiple source schemas to define the same field, ensuring that this decision is both intentional and coordinated by requiring fields to be explicitly marked."
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Multiple_By_Fields_Should_Be_Turned_Into_Lookups()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            type Query {
              productById(id: ID!): Product
              productByName(name: String!): Product
            }

            type Product {
              id: ID!
              other: String
              name: String!
            }
            """);
        var preprocessor = new SourceSchemaPreprocessor(schema);

        // act
        var result = preprocessor.Process();

        // assert
        SchemaFormatter.FormatAsString(result).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @shareable {
              productById(id: ID!): Product
                @lookup
              productByName(name: String!): Product
                @lookup
            }

            type Product
              @key(fields: "id")
              @key(fields: "name")
              @shareable {
              id: ID!
              name: String!
              other: String
            }

            scalar FieldSelectionSet

            "The @key directive is used to designate an entity’s unique key, which identifies how to uniquely reference an instance of an entity across different source schemas."
            directive @key("Represents a selection set syntax." fields: FieldSelectionSet!) repeatable on OBJECT | INTERFACE

            "The @lookup directive is used within a source schema to specify output fields that can be used by the distributed GraphQL executor to resolve an entity by a stable key."
            directive @lookup on FIELD_DEFINITION

            "The @shareable directive allows multiple source schemas to define the same field, ensuring that this decision is both intentional and coordinated by requiring fields to be explicitly marked."
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            """);
    }

    [Fact]
    public void By_Field_Should_Not_Be_Turned_Into_Lookup_If_Field_Does_Not_Exist_On_Result_Type()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            type Query {
              productByGtin(gtin: String!): Product
            }

            type Product {
              id: ID!
              other: String
            }
            """);
        var preprocessor = new SourceSchemaPreprocessor(schema);

        // act
        var result = preprocessor.Process();

        // assert
        SchemaFormatter.FormatAsString(result).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @shareable {
              productByGtin(gtin: String!): Product
            }

            type Product
              @shareable {
              id: ID!
              other: String
            }

            "The @shareable directive allows multiple source schemas to define the same field, ensuring that this decision is both intentional and coordinated by requiring fields to be explicitly marked."
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            """);
    }

    [Fact]
    public void By_Field_With_Non_Null_Result_Type_Should_Not_Be_Turned_Into_Lookup()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            type Query {
              productById(id: ID!): Product!
            }

            type Product {
              id: ID!
              other: String
            }
            """);
        var preprocessor = new SourceSchemaPreprocessor(schema);

        // act
        var result = preprocessor.Process();

        // assert
        SchemaFormatter.FormatAsString(result).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @shareable {
              productById(id: ID!): Product!
            }

            type Product
              @shareable {
              id: ID!
              other: String
            }

            "The @shareable directive allows multiple source schemas to define the same field, ensuring that this decision is both intentional and coordinated by requiring fields to be explicitly marked."
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            """);
    }

    [Fact]
    public void By_Field_With_Multiple_Arguments_Should_Not_Be_Turned_Into_Lookup()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            type Query {
              productById(id: ID! other: String): Product
            }

            type Product {
              id: ID!
              other: String
            }
            """);
        var preprocessor = new SourceSchemaPreprocessor(schema);

        // act
        var result = preprocessor.Process();

        // assert
        SchemaFormatter.FormatAsString(result).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @shareable {
              productById(id: ID! other: String): Product
            }

            type Product
              @shareable {
              id: ID!
              other: String
            }

            "The @shareable directive allows multiple source schemas to define the same field, ensuring that this decision is both intentional and coordinated by requiring fields to be explicitly marked."
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            """);
    }

    [Fact]
    public void By_Field_With_List_Result_Type_Should_Not_Be_Turned_Into_Lookup()
    {
        // arrange
        var schema = SchemaParser.Parse(
            """
            type Query {
              productById(ids: [ID!]!): [Product]
            }

            type Product {
              id: ID!
            }
            """);
        var preprocessor = new SourceSchemaPreprocessor(schema);

        // act
        var result = preprocessor.Process();

        // assert
        SchemaFormatter.FormatAsString(result).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @shareable {
              productById(ids: [ID!]!): [Product]
            }

            type Product
              @shareable {
              id: ID!
            }

            "The @shareable directive allows multiple source schemas to define the same field, ensuring that this decision is both intentional and coordinated by requiring fields to be explicitly marked."
            directive @shareable repeatable on OBJECT | FIELD_DEFINITION
            """);
    }
}
