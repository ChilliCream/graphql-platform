using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Language;

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

    [Fact]
    public void Rewrite_EntityLookup_Should_IncludeInlineFragment()
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
        var result = rewriter.GetOrRewrite(sourceText, 77777UL);

        // assert
        Assert.True(result.IsEntityLookup);
        Assert.NotNull(result.InlineFragment);
        Assert.Equal("Product", result.InlineFragment!.TypeCondition!.Name.Value);

        var selectionNames = result.InlineFragment.SelectionSet.Selections
            .OfType<FieldNode>()
            .Select(f => f.Name.Value)
            .ToArray();
        Assert.Contains("id", selectionNames);
        Assert.Contains("name", selectionNames);
        Assert.Contains("price", selectionNames);
    }

    [Fact]
    public void Rewrite_Passthrough_Should_HaveNullInlineFragment()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>();
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query {
              topProducts { id name }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 88888UL);

        // assert
        Assert.False(result.IsEntityLookup);
        Assert.Null(result.InlineFragment);
    }

    [Fact]
    public void BuildCombinedEntityQuery_Should_ProduceAliasedEntitiesQuery()
    {
        // arrange
        var productLookup = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };
        var userLookup = new Dictionary<string, LookupFieldInfo>
        {
            ["userByEmail"] = new LookupFieldInfo
            {
                EntityTypeName = "User",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["email"] = "email" }
            }
        };

        var productRewriter = new FederationQueryRewriter(productLookup);
        var userRewriter = new FederationQueryRewriter(userLookup);

        var productOp = productRewriter.GetOrRewrite(
            """
            query($__fusion_1_id: ID!) {
              productById(id: $__fusion_1_id) { id name price }
            }
            """,
            1UL);

        var userOp = userRewriter.GetOrRewrite(
            """
            query($__fusion_1_email: String!) {
              userByEmail(email: $__fusion_1_email) { email name }
            }
            """,
            2UL);

        var requests = ImmutableArray.Create(
            new SourceSchemaClientRequest
            {
                Node = null!,
                SchemaName = "test",
                OperationType = OperationType.Query,
                OperationSourceText = productOp.OperationText,
                OperationHash = 1UL,
                Variables = []
            },
            new SourceSchemaClientRequest
            {
                Node = null!,
                SchemaName = "test",
                OperationType = OperationType.Query,
                OperationSourceText = userOp.OperationText,
                OperationHash = 2UL,
                Variables = []
            });

        var rewrittenOps = new[] { productOp, userOp };

        // act
        var (queryText, variablesJson) =
            ApolloFederationSourceSchemaClient.BuildCombinedEntityQuery(requests, rewrittenOps);

        // assert — query structure
        Assert.Contains("$r0: [_Any!]!", queryText);
        Assert.Contains("$r1: [_Any!]!", queryText);
        Assert.Contains("____request0: _entities(representations: $r0)", queryText);
        Assert.Contains("____request1: _entities(representations: $r1)", queryText);
        Assert.Contains("... on Product", queryText);
        Assert.Contains("... on User", queryText);

        // assert — variables structure
        Assert.Contains("\"r0\"", variablesJson);
        Assert.Contains("\"r1\"", variablesJson);
        Assert.Contains("\"__typename\":\"Product\"", variablesJson);
        Assert.Contains("\"__typename\":\"User\"", variablesJson);
    }
}
