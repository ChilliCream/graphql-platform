using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
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
        var sourceSchemaParser = new SourceSchemaParser([sourceSchemaText], new CompositionLog());
        var schema = sourceSchemaParser.Parse().Value.Single();
        var enricher = new SourceSchemaEnricher(schema);

        // act
        enricher.Enrich();

        var productType = (MutableObjectTypeDefinition)schema.Types["Product"];
        var productIdFieldMetadata = productType.Fields["id"].GetSourceFieldMetadata();
        var productCategoryFieldMetadata = productType.Fields["category"].GetSourceFieldMetadata();
        var productSkuFieldMetadata = productType.Fields["sku"].GetSourceFieldMetadata();
        var productAlreadyShareableFieldMetadata = productType.Fields["alreadyShareable"].GetSourceFieldMetadata();
        var productExternalFieldMetadata = productType.Fields["externalField"].GetSourceFieldMetadata();
        var productNonKeyFieldMetadata = productType.Fields["nonKeyField"].GetSourceFieldMetadata();
        var categoryType = (MutableInterfaceTypeDefinition)schema.Types["Category"];
        var categoryIdFieldMetadata = categoryType.Fields["id"].GetSourceFieldMetadata();
        var bookCategoryType = (MutableObjectTypeDefinition)schema.Types["BookCategory"];
        var bookCategoryIdFieldMetadata = bookCategoryType.Fields["id"].GetSourceFieldMetadata();
        var bookCategoryCountFieldMetadata = bookCategoryType.Fields["count"].GetSourceFieldMetadata();

        // assert
        Assert.True(productIdFieldMetadata?.IsKeyField);
        Assert.True(productIdFieldMetadata?.IsShareable);
        Assert.True(productCategoryFieldMetadata?.IsKeyField);
        Assert.True(productCategoryFieldMetadata?.IsShareable);
        Assert.True(productSkuFieldMetadata?.IsKeyField);
        Assert.True(productSkuFieldMetadata?.IsShareable);
        Assert.True(productAlreadyShareableFieldMetadata?.IsKeyField);
        Assert.True(productAlreadyShareableFieldMetadata?.IsShareable);
        Assert.True(productExternalFieldMetadata?.IsKeyField);
        Assert.False(productExternalFieldMetadata?.IsShareable);
        Assert.False(productNonKeyFieldMetadata?.IsKeyField);
        Assert.False(productNonKeyFieldMetadata?.IsShareable);
        Assert.True(categoryIdFieldMetadata?.IsKeyField);
        Assert.True(categoryIdFieldMetadata?.IsShareable);
        Assert.False(bookCategoryIdFieldMetadata?.IsKeyField);
        Assert.True(bookCategoryIdFieldMetadata?.IsShareable);
        Assert.False(bookCategoryCountFieldMetadata?.IsKeyField);
        Assert.True(bookCategoryCountFieldMetadata?.IsShareable);
    }
}
