using HotChocolate.Language;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.Utilities;

public class SchemaHelperTests
{
    [Fact]
    public void LoadGitHubSchema()
    {
        // arrange
        var schemaSdl = FileResource.Open("GitHub.graphql");
        const string extensionsSdl = @"extend schema @key(fields: ""id"")";

        // act
        var schema =
            SchemaHelper.Load(
                [
                    new("GitHub.graphql", Utf8GraphQLParser.Parse(schemaSdl)),
                    new("GitHub.extensions.graphql", Utf8GraphQLParser.Parse(extensionsSdl))
                ]);

        // assert
        var scalarType = schema.Types.GetType<ScalarType>("X509Certificate");

        Assert.Equal(
            "global::System.String",
            scalarType.GetSerializationType());

        Assert.Equal(
            "global::System.String",
            scalarType.GetRuntimeType());
    }

    [Fact]
    public void Load_DuplicateTypesAcrossFiles_DeduplicatesByName()
    {
        // arrange - simulates a scenario where the same type definition appears
        // in multiple schema files (e.g., overlapping glob patterns or Hygraph schemas)
        const string schema1 = """
            interface Entity {
                id: ID!
                stage: Stage!
            }

            enum Stage { DRAFT PUBLISHED }

            type FooComponent implements Entity {
                id: ID!
                stage: Stage!
                title: String
            }

            input FooComponentUpsertInput {
                create: FooComponentCreateInput!
                update: FooComponentUpdateInput!
            }

            input FooComponentCreateInput { title: String }
            input FooComponentUpdateInput { title: String }

            type Query {
                foos: [FooComponent!]!
            }
            """;

        // Second file contains some of the same types (simulating overlapping files)
        const string schema2 = """
            input FooComponentUpsertInput {
                create: FooComponentCreateInput!
                update: FooComponentUpdateInput!
            }

            input FooComponentCreateInput { title: String }
            input FooComponentUpdateInput { title: String }
            """;

        const string extensionsSdl = @"extend schema @key(fields: ""id"")";

        // act - should load successfully and deduplicate by name (first occurrence wins)
        var schema = SchemaHelper.Load(
            [
                new("schema1.graphql", Utf8GraphQLParser.Parse(schema1)),
                new("schema2.graphql", Utf8GraphQLParser.Parse(schema2)),
                new("extensions.graphql", Utf8GraphQLParser.Parse(extensionsSdl))
            ],
            strictValidation: false);

        // assert - schema loaded successfully with the types
        Assert.NotNull(schema.Types.GetType<InputObjectType>("FooComponentUpsertInput"));
        Assert.NotNull(schema.Types.GetType<InputObjectType>("FooComponentCreateInput"));
        Assert.NotNull(schema.Types.GetType<ObjectType>("FooComponent"));
    }

    [Fact]
    public void Load_MixedExtensionAndDefinitionFile_ProcessesBoth()
    {
        // arrange - a file that contains both extension nodes and regular type definitions
        const string schemaSdl2 = """
            type Query {
                foos: [Foo!]!
            }

            type Foo {
                id: ID!
                name: String
            }
            """;

        // This file has both an extension AND regular type definitions
        const string mixedSdl = """
            extend schema @key(fields: "id")

            input FooInput {
                name: String!
            }
            """;

        // act
        var schema = SchemaHelper.Load(
            [
                new("schema.graphql", Utf8GraphQLParser.Parse(schemaSdl2)),
                new("mixed.graphql", Utf8GraphQLParser.Parse(mixedSdl))
            ],
            strictValidation: false);

        // assert - both the extension AND the input type should be processed
        Assert.NotNull(schema.Types.GetType<InputObjectType>("FooInput"));
        Assert.NotNull(schema.Types.GetType<ObjectType>("Foo"));
    }
}
