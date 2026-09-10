using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.ApolloFederation;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Logging;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerConnectorKindTests
{
    [Fact]
    public void Merge_Should_LiftConnectorKind_OntoSchemaMetadata_When_FeaturePresent()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "Products",
                """
                type Query {
                  productById(id: ID!): Product @lookup
                }

                type Product @key(fields: "id") {
                  id: ID!
                }
                """);
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        schema.Features.Set(new ConnectorKindMetadata("ApolloFederation"));
        var schemas = ImmutableSortedSet.Create(
            new SchemaByNameComparer<MutableSchemaDefinition>(), schema);
        new SourceSchemaEnricher(schema, schemas).Enrich();
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ApolloFederation", GetConnectorKind(result.Value, "PRODUCTS"));
    }

    [Fact]
    public void Merge_Should_OmitMetadataKind_When_FeatureAbsent()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "Catalog",
                """
                type Query {
                  ping: String
                }
                """);
        var compositionLog = new CompositionLog();
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, compositionLog);
        var schema = sourceSchemaParser.Parse().Value;
        var schemas = ImmutableSortedSet.Create(
            new SchemaByNameComparer<MutableSchemaDefinition>(), schema);
        new SourceSchemaEnricher(schema, schemas).Enrich();
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Null(GetConnectorKindOrDefault(result.Value, "CATALOG"));
    }

    [Fact]
    public void Merge_Should_LiftPerSchemaConnectorKind_When_MixedSources()
    {
        // arrange
        var apolloSource = new SourceSchemaText(
            "Reviews",
            """
            type Query {
              reviewById(id: ID!): Review @lookup
            }

            type Review @key(fields: "id") {
              id: ID!
            }
            """);
        var graphqlSource = new SourceSchemaText(
            "Catalog",
            """
            type Query {
              ping: String
            }
            """);
        var compositionLog = new CompositionLog();
        var schemaA = new SourceSchemaParser(apolloSource, compositionLog).Parse().Value;
        schemaA.Features.Set(new ConnectorKindMetadata("ApolloFederation"));
        var schemaB = new SourceSchemaParser(graphqlSource, compositionLog).Parse().Value;
        var schemas = ImmutableSortedSet.Create(
            new SchemaByNameComparer<MutableSchemaDefinition>(), schemaA, schemaB);
        new SourceSchemaEnricher(schemaA, schemas).Enrich();
        new SourceSchemaEnricher(schemaB, schemas).Enrich();
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Equal("ApolloFederation", GetConnectorKind(result.Value, "REVIEWS"));
        Assert.Null(GetConnectorKindOrDefault(result.Value, "CATALOG"));
    }

    private static string GetConnectorKind(MutableSchemaDefinition mergedSchema, string valueName)
    {
        var directive = GetSchemaMetadataDirective(mergedSchema, valueName);
        Assert.NotNull(directive);
        Assert.True(directive.Arguments.TryGetValue("kind", out var kindValue));
        return Assert.IsType<StringValueNode>(kindValue).Value;
    }

    private static string? GetConnectorKindOrDefault(MutableSchemaDefinition mergedSchema, string valueName)
    {
        var directive = GetSchemaMetadataDirective(mergedSchema, valueName);

        if (directive?.Arguments.TryGetValue("kind", out var kindValue) == true)
        {
            return Assert.IsType<StringValueNode>(kindValue).Value;
        }

        return null;
    }

    private static Directive? GetSchemaMetadataDirective(
        MutableSchemaDefinition mergedSchema,
        string valueName)
    {
        var schemaEnum = Assert.IsAssignableFrom<MutableEnumTypeDefinition>(
            mergedSchema.Types["fusion__Schema"]);
        Assert.True(schemaEnum.Values.TryGetValue(valueName, out var enumValue));
        return enumValue.Directives.FirstOrDefault("fusion__schema_metadata");
    }
}
