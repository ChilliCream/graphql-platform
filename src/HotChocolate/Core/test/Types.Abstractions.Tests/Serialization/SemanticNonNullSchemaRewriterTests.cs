using HotChocolate.Language;

namespace HotChocolate.Serialization;

public class SemanticNonNullSchemaRewriterTests
{
    [Fact]
    public void Rewrite_Should_Return_Same_Document_When_No_NonNull_Fields()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hero: Character
            }

            type Character {
              name: String
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hero: Character
            }

            type Character {
              name: String
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_Strip_NonNull_And_Add_Directive_When_Field_Is_NonNull()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              name: String!
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              name: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Compute_Multiple_Levels_When_NonNull_List_Items()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              tags: [String!]!
              matrix: [[String!]!]!
              innerOnly: [[String!]]
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              tags: [String] @semanticNonNull(levels: [0, 1])
              matrix: [[String]] @semanticNonNull(levels: [0, 1, 2])
              innerOnly: [[String]] @semanticNonNull(levels: [2])
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Skip_Mutation_Type_When_Declared_In_Schema_Definition()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            schema {
              query: Query
              mutation: MyMutation
            }

            type Query {
              hello: String!
            }

            type MyMutation {
              doStuff: String!
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            schema {
              query: Query
              mutation: MyMutation
            }

            type Query {
              hello: String @semanticNonNull
            }

            type MyMutation {
              doStuff: String!
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Skip_Mutation_Type_By_Convention_When_No_Schema_Definition()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hello: String!
            }

            type Mutation {
              doStuff: String!
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hello: String @semanticNonNull
            }

            type Mutation {
              doStuff: String!
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Skip_PageInfo_And_CollectionSegmentInfo_Object_Types()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hero: String!
            }

            type PageInfo {
              hasNextPage: Boolean!
            }

            type CollectionSegmentInfo {
              hasNextPage: Boolean!
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hero: String @semanticNonNull
            }

            type PageInfo {
              hasNextPage: Boolean!
            }

            type CollectionSegmentInfo {
              hasNextPage: Boolean!
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Skip_Node_Interface_And_Id_Fields()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hero: Character!
            }

            type Character {
              id: ID!
              name: String!
            }

            interface Node {
              id: ID!
            }

            interface HasName {
              id: ID!
              name: String!
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hero: Character @semanticNonNull
            }

            type Character {
              id: ID!
              name: String @semanticNonNull
            }

            interface Node {
              id: ID!
            }

            interface HasName {
              id: ID!
              name: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Skip_Introspection_Types_And_Fields()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hero: String!
              __typename: String!
            }

            type __Schema {
              types: [String!]!
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hero: String @semanticNonNull
              __typename: String!
            }

            type __Schema {
              types: [String!]!
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Preserve_Existing_Field_Directives_When_Adding_SemanticNonNull()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hello: String! @deprecated(reason: "old")
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hello: String @deprecated(reason: "old") @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Insert_Directive_Alphabetically_When_Existing_Directive_Definitions()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hello: String!
            }

            directive @cost(weight: String!) on FIELD_DEFINITION
            directive @listSize(assumedSize: Int) on FIELD_DEFINITION
            directive @stream on FIELD
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hello: String @semanticNonNull
            }

            directive @cost(weight: String!) on FIELD_DEFINITION

            directive @listSize(assumedSize: Int) on FIELD_DEFINITION

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION

            directive @stream on FIELD
            """);
    }

    [Fact]
    public void Rewrite_Should_Insert_Directive_Between_Enums_And_Scalars_When_No_Existing_Directives()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hello: String!
            }

            enum Episode {
              NEW_HOPE
            }

            scalar DateTime
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hello: String @semanticNonNull
            }

            enum Episode {
              NEW_HOPE
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION

            scalar DateTime
            """);
    }

    [Fact]
    public void Rewrite_Should_Not_Duplicate_Directive_Definition_When_Already_Defined()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              hello: String!
            }

            directive @semanticNonNull(levels: [Int!] = [0]) on FIELD_DEFINITION
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              hello: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }

    [Fact]
    public void Rewrite_Should_Replace_Existing_SemanticNonNull_Directive_On_Field()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              tags: [String!]! @semanticNonNull(levels: [42])
            }
            """);

        // act
        var result = SemanticNonNullSchemaRewriter.Rewrite(schema);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              tags: [String] @semanticNonNull(levels: [0, 1])
            }

            directive @semanticNonNull(levels: [Int!] = [
              0
            ]) on FIELD_DEFINITION
            """);
    }
}
