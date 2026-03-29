using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion;

public class ApolloFederationConnectorTests
{
    [Fact]
    public void Configuration_Should_StoreProperties()
    {
        // arrange & act
        var config = new ApolloFederationSourceSchemaClientConfiguration(
            "products",
            "products-http",
            SupportedOperationType.Query);

        // assert
        Assert.Equal("products", config.Name);
        Assert.Equal("products-http", config.HttpClientName);
        Assert.Equal(SupportedOperationType.Query, config.SupportedOperations);
    }

    [Fact]
    public void Configuration_Should_DefaultToQueryAndMutation()
    {
        // arrange & act
        var config = new ApolloFederationSourceSchemaClientConfiguration(
            "products",
            "products-http");

        // assert
        Assert.Equal(
            SupportedOperationType.Query | SupportedOperationType.Mutation,
            config.SupportedOperations);
    }

    [Fact]
    public void Rewrite_SimpleLookup_Should_ProduceEntitiesQuery()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query GetProduct($__fusion_1_id: ID!) {
              productById(id: $__fusion_1_id) {
                id
                name
                price
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 12345UL);

        // assert
        Assert.True(result.IsEntityLookup);
        Assert.Equal("Product", result.EntityTypeName);
        Assert.Contains("_entities", result.OperationText);
        Assert.Contains("representations", result.OperationText);
        Assert.Contains("... on Product", result.OperationText);
        Assert.Contains("name", result.OperationText);
        Assert.Contains("price", result.OperationText);
        Assert.Equal("id", result.VariableToKeyFieldMap["__fusion_1_id"]);
    }

    [Fact]
    public void Rewrite_CompositeKeyLookup_Should_MapMultipleArguments()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productBySkuAndPackage"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string>
                {
                    ["sku"] = "sku",
                    ["package"] = "package"
                }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query Op($__fusion_1_sku: String!, $__fusion_1_package: String!) {
              productBySkuAndPackage(sku: $__fusion_1_sku, package: $__fusion_1_package) {
                sku
                package
                name
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 12346UL);

        // assert
        Assert.True(result.IsEntityLookup);
        Assert.Equal("Product", result.EntityTypeName);
        Assert.Equal("sku", result.VariableToKeyFieldMap["__fusion_1_sku"]);
        Assert.Equal("package", result.VariableToKeyFieldMap["__fusion_1_package"]);
    }

    [Fact]
    public void Rewrite_NonLookupField_Should_BePassthrough()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>();
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query {
              topProducts {
                id
                name
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 12347UL);

        // assert
        Assert.False(result.IsEntityLookup);
        Assert.Null(result.EntityTypeName);
        Assert.Null(result.LookupFieldName);
        Assert.DoesNotContain("_entities", result.OperationText);
    }

    [Fact]
    public void GetOrRewrite_SameHash_Should_ReturnCachedResult()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query Op($__fusion_1_id: ID!) {
              productById(id: $__fusion_1_id) { id name }
            }
            """;

        // act
        var result1 = rewriter.GetOrRewrite(sourceText, 99UL);
        var result2 = rewriter.GetOrRewrite(sourceText, 99UL);

        // assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void Rewrite_SimpleLookup_Should_ProduceCorrectVariableDefinition()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query GetProduct($__fusion_1_id: ID!) {
              productById(id: $__fusion_1_id) {
                id
                name
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 55555UL);

        // assert — the rewritten query should declare $representations: [_Any!]!
        Assert.Contains("$representations: [_Any!]!", result.OperationText);
        Assert.Equal("productById", result.LookupFieldName);
    }

    [Fact]
    public void Rewrite_DifferentHashes_Should_ReturnDistinctResults()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query Op($__fusion_1_id: ID!) {
              productById(id: $__fusion_1_id) { id name }
            }
            """;

        // act
        var result1 = rewriter.GetOrRewrite(sourceText, 100UL);
        var result2 = rewriter.GetOrRewrite(sourceText, 200UL);

        // assert — different hash keys produce separate cache entries
        Assert.NotSame(result1, result2);
        // but the content should be equivalent since the source text is the same
        Assert.Equal(result1.OperationText, result2.OperationText);
    }
}
