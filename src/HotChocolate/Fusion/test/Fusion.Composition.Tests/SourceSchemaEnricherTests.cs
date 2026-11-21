using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Fusion.Logging;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaEnricherTests
{
    [Fact]
    public void Enrich_SourceSchema_SetsIsKeyFieldAndIsShareableMetadata()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                """
                type Product
                    @key(fields: "id, category { id }")
                    @key(fields: "sku")
                    @key(fields: "alreadyShareable")
                    @key(fields: "externalField") {
                    id: ID!
                    category: Category!
                    sku: String!
                    alreadyShareable: String! @shareable
                    externalField: String! @external
                    nonKeyField: String
                }

                interface Category {
                    id: ID!
                }

                type BookCategory implements Category @shareable {
                    id: ID!
                    count: Int!
                }
                """);
        var sourceSchemaParser = new SourceSchemaParser(sourceSchemaText, new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value;
        var enricher = new SourceSchemaEnricher(schema, [schema]);

        // act
        enricher.Enrich();

        static SourceOutputFieldMetadata GetMetadata(IOutputFieldDefinition field) =>
            field.Features.GetRequired<SourceOutputFieldMetadata>();

        var productType = (MutableObjectTypeDefinition)schema.Types["Product"];
        var productIdFieldMetadata = GetMetadata(productType.Fields["id"]);
        var productCategoryFieldMetadata = GetMetadata(productType.Fields["category"]);
        var productSkuFieldMetadata = GetMetadata(productType.Fields["sku"]);
        var productAlreadyShareableFieldMetadata = GetMetadata(productType.Fields["alreadyShareable"]);
        var productExternalFieldMetadata = GetMetadata(productType.Fields["externalField"]);
        var productNonKeyFieldMetadata = GetMetadata(productType.Fields["nonKeyField"]);
        var categoryType = (MutableInterfaceTypeDefinition)schema.Types["Category"];
        var categoryIdFieldMetadata = GetMetadata(categoryType.Fields["id"]);
        var bookCategoryType = (MutableObjectTypeDefinition)schema.Types["BookCategory"];
        var bookCategoryIdFieldMetadata = GetMetadata(bookCategoryType.Fields["id"]);
        var bookCategoryCountFieldMetadata = GetMetadata(bookCategoryType.Fields["count"]);

        // assert
        Assert.True(productIdFieldMetadata.IsKeyField);
        Assert.True(productIdFieldMetadata.IsShareable);
        Assert.True(productCategoryFieldMetadata.IsKeyField);
        Assert.True(productCategoryFieldMetadata.IsShareable);
        Assert.True(productSkuFieldMetadata.IsKeyField);
        Assert.True(productSkuFieldMetadata.IsShareable);
        Assert.True(productAlreadyShareableFieldMetadata.IsKeyField);
        Assert.True(productAlreadyShareableFieldMetadata.IsShareable);
        Assert.True(productExternalFieldMetadata.IsKeyField);
        Assert.False(productExternalFieldMetadata.IsShareable);
        Assert.False(productNonKeyFieldMetadata.IsKeyField);
        Assert.False(productNonKeyFieldMetadata.IsShareable);
        Assert.True(categoryIdFieldMetadata.IsKeyField);
        Assert.True(categoryIdFieldMetadata.IsShareable);
        Assert.False(bookCategoryIdFieldMetadata.IsKeyField);
        Assert.True(bookCategoryIdFieldMetadata.IsShareable);
        Assert.False(bookCategoryCountFieldMetadata.IsKeyField);
        Assert.True(bookCategoryCountFieldMetadata.IsShareable);
    }

    [Fact]
    public void Enrich_RequireWithContextPath_SetsContextNames()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    price(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int
                }
                """);
        var schema = new SourceSchemaParser(sourceSchemaText, new CompositionLog()).Parse().Value;
        var enricher = new SourceSchemaEnricher(schema, [schema]);

        // act
        enricher.Enrich();

        // assert
        var argument = schema.QueryType!.Fields["price"].Arguments["currency"];
        var metadata = argument.Features.GetRequired<SourceInputFieldMetadata>();
        Assert.Equal(["userContext"], metadata.RequireInfo!.ContextNames);
    }

    [Fact]
    public void Enrich_RequireWithNestedContextPaths_SetsDistinctContextNames()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    price(
                        input: Input!
                            @require(field: "{ a: $userContext.currency, b: $region.code, c: $userContext.id, d: id }")): Int
                }

                input Input {
                    a: String
                    b: String
                    c: String
                    d: String
                }
                """);
        var schema = new SourceSchemaParser(sourceSchemaText, new CompositionLog()).Parse().Value;
        var enricher = new SourceSchemaEnricher(schema, [schema]);

        // act
        enricher.Enrich();

        // assert
        var argument = schema.QueryType!.Fields["price"].Arguments["input"];
        var metadata = argument.Features.GetRequired<SourceInputFieldMetadata>();
        Assert.Equal(["userContext", "region"], metadata.RequireInfo!.ContextNames);
    }

    [Fact]
    public void Enrich_RequireWithoutContextPath_SetsEmptyContextNames()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    price(currency: String! @require(field: "currency.isoCode")): Int
                }
                """);
        var schema = new SourceSchemaParser(sourceSchemaText, new CompositionLog()).Parse().Value;
        var enricher = new SourceSchemaEnricher(schema, [schema]);

        // act
        enricher.Enrich();

        // assert
        var argument = schema.QueryType!.Fields["price"].Arguments["currency"];
        var metadata = argument.Features.GetRequired<SourceInputFieldMetadata>();
        Assert.Empty(metadata.RequireInfo!.ContextNames);
    }

    [Fact]
    public void Enrich_RequireWithInvalidSyntax_SetsEmptyContextNames()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    price(currency: String! @require(field: "$userContext.")): Int
                }
                """);
        var schema = new SourceSchemaParser(sourceSchemaText, new CompositionLog()).Parse().Value;
        var enricher = new SourceSchemaEnricher(schema, [schema]);

        // act
        enricher.Enrich();

        // assert
        var argument = schema.QueryType!.Fields["price"].Arguments["currency"];
        var requireInfo = argument.Features.GetRequired<SourceInputFieldMetadata>().RequireInfo!;
        Assert.True(requireInfo.IsInvalidFieldSyntax);
        Assert.Empty(requireInfo.ContextNames);
    }

    [Fact]
    public void Enrich_IsWithContextPath_SetsContextNames()
    {
        // arrange
        var sourceSchemaText =
            new SourceSchemaText(
                "A",
                // lang=graphql
                """
                type Query {
                    product(id: ID! @is(field: "$userContext.productId")): String
                }
                """);
        var schema = new SourceSchemaParser(sourceSchemaText, new CompositionLog()).Parse().Value;
        var enricher = new SourceSchemaEnricher(schema, [schema]);

        // act
        enricher.Enrich();

        // assert
        var argument = schema.QueryType!.Fields["product"].Arguments["id"];
        var metadata = argument.Features.GetRequired<SourceInputFieldMetadata>();
        Assert.Equal(["userContext"], metadata.IsInfo!.ContextNames);
    }
}
