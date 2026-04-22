using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

public class ApolloFederationConnectorTests
{
    [Fact]
    public void Configuration_Should_StoreProperties()
    {
        // arrange
        var baseAddress = new Uri("http://localhost:5000/graphql");
        var lookups = new Dictionary<string, LookupFieldInfo>();

        // act
        var config = new ApolloFederationSourceSchemaClientConfiguration(
            "products",
            "products-http",
            baseAddress,
            lookups,
            supportedOperations: SupportedOperationType.Query);

        // assert
        Assert.Equal("products", config.Name);
        Assert.Equal("products-http", config.HttpClientName);
        Assert.Same(baseAddress, config.BaseAddress);
        Assert.Same(lookups, config.Lookups);
        Assert.Equal(SupportedOperationType.Query, config.SupportedOperations);
    }

    [Fact]
    public void Configuration_Should_DefaultToQueryAndMutation()
    {
        // arrange & act
        var config = new ApolloFederationSourceSchemaClientConfiguration(
            "products",
            "products-http",
            new Uri("http://localhost:5000/graphql"),
            new Dictionary<string, LookupFieldInfo>());

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

        // assert: the rewritten query should declare $representations: [_Any!]!
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

        // assert: different hash keys produce separate cache entries
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

        // assert: query structure
        Assert.Contains("$r0: [_Any!]!", queryText);
        Assert.Contains("$r1: [_Any!]!", queryText);
        Assert.Contains("____request0: _entities(representations: $r0)", queryText);
        Assert.Contains("____request1: _entities(representations: $r1)", queryText);
        Assert.Contains("... on Product", queryText);
        Assert.Contains("... on User", queryText);

        // assert: variables structure
        Assert.Contains("\"r0\"", variablesJson);
        Assert.Contains("\"r1\"", variablesJson);
        Assert.Contains("\"__typename\":\"Product\"", variablesJson);
        Assert.Contains("\"__typename\":\"User\"", variablesJson);
    }

    [Fact]
    public void Rewrite_NestedObjectLookup_Should_MapArgumentToKeyPath()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            // Apollo Federation '@key(fields: "metadata { id }")' composed into
            // a single wrapper argument whose JSON is splatted into the
            // '_entities' representation root.
            ["articleByMetadata"] = new LookupFieldInfo
            {
                EntityTypeName = "Article",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["key"] = string.Empty }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query GetArticle($__fusion_1_key: ArticleByMetadataInput!) {
              articleByMetadata(key: $__fusion_1_key) {
                title
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 101UL);

        // assert
        Assert.True(result.IsEntityLookup);
        Assert.Equal("Article", result.EntityTypeName);
        Assert.Equal("articleByMetadata", result.LookupFieldName);
        Assert.Equal(string.Empty, result.VariableToKeyFieldMap["__fusion_1_key"]);
        Assert.Contains("... on Article", result.OperationText);
        Assert.Contains("_entities", result.OperationText);
    }

    [Fact]
    public void Rewrite_NestedListLookup_Should_MapArgumentToKeyPath()
    {
        // arrange
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            // Apollo Federation '@key(fields: "products { id }")' composed
            // into a wrapper argument whose JSON (containing a 'products'
            // list) is splatted into the '_entities' representation root.
            ["productListByProductsAndId"] = new LookupFieldInfo
            {
                EntityTypeName = "ProductList",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["key"] = string.Empty }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query GetList($__fusion_1_key: ProductListByProductsAndIdInput!) {
              productListByProductsAndId(key: $__fusion_1_key) {
                products { id }
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 102UL);

        // assert
        Assert.True(result.IsEntityLookup);
        Assert.Equal("ProductList", result.EntityTypeName);
        Assert.Equal("productListByProductsAndId", result.LookupFieldName);
        Assert.Equal(string.Empty, result.VariableToKeyFieldMap["__fusion_1_key"]);
    }

    [Fact]
    public void Rewrite_DeeplyNestedListLookup_Should_MapArgumentToKeyPath()
    {
        // arrange: mirrors the audit's 'price' subgraph '@key(fields:
        // "products { id pid category { id tag } } selected { id }")'.
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndId"]
                = new LookupFieldInfo
                {
                    EntityTypeName = "ProductList",
                    ArgumentToKeyFieldMap =
                        new Dictionary<string, string> { ["key"] = string.Empty }
                }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query($__fusion_1_key: ProductListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndIdInput!) {
              productListByProductsAndIdAndPidAndCategoryAndIdAndTagAndSelectedAndId(
                key: $__fusion_1_key
              ) {
                selected { id }
                first { id }
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 103UL);

        // assert
        Assert.True(result.IsEntityLookup);
        Assert.Equal("ProductList", result.EntityTypeName);
        Assert.Equal(string.Empty, result.VariableToKeyFieldMap["__fusion_1_key"]);
        Assert.Contains("... on ProductList", result.OperationText);
    }

    [Fact]
    public void BuildCombinedEntityQuery_Should_ProduceEntitiesAliasForNestedLookup()
    {
        // arrange: a wrapper-shape argument lookup generated for a nested
        // '@key' is rewritten into an '_entities(...)' query that accepts the
        // wrapper's variable JSON as-is.
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productListByProductsAndId"] = new LookupFieldInfo
            {
                EntityTypeName = "ProductList",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["key"] = string.Empty }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields);

        const string sourceText = """
            query($__fusion_1_key: ProductListByProductsAndIdInput!) {
              productListByProductsAndId(key: $__fusion_1_key) {
                products { id }
              }
            }
            """;

        var rewritten = rewriter.GetOrRewrite(sourceText, 555UL);

        var requests = ImmutableArray.Create(
            new SourceSchemaClientRequest
            {
                Node = null!,
                SchemaName = "list",
                OperationType = OperationType.Query,
                OperationSourceText = rewritten.OperationText,
                OperationHash = 555UL,
                Variables = []
            });

        var rewrittenOps = new[] { rewritten };

        // act
        var (queryText, variablesJson) =
            ApolloFederationSourceSchemaClient.BuildCombinedEntityQuery(requests, rewrittenOps);

        // assert: the batched query shape uses '_entities' and carries the
        // entity '__typename' in its representations, exactly as for a flat
        // scalar lookup.
        Assert.Contains("$r0: [_Any!]!", queryText);
        Assert.Contains("____request0: _entities(representations: $r0)", queryText);
        Assert.Contains("... on ProductList", queryText);
        Assert.Contains("\"__typename\":\"ProductList\"", variablesJson);
    }

    [Fact]
    public void Rewrite_Should_Strip_RequireArgument_And_Record_RepresentationMapping()
    {
        // arrange: the composer translates '@requires(fields: "price")' on
        // 'Product.isExpensive' into a synthetic 'price' argument on the
        // composite-schema field. The planner emits the argument as a
        // variable reference, and the rewriter must strip it from the
        // outgoing '_entities' selection while recording the bound variable
        // against the representation field 'price'.
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };
        var entityRequires = new Dictionary<string, EntityRequiresInfo>
        {
            ["Product"] = new EntityRequiresInfo
            {
                Fields = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    ["isExpensive"] = new Dictionary<string, string> { ["price"] = "price" }
                }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields, entityRequires);

        const string sourceText = """
            query($__fusion_1_id: ID!, $__fusion_2_price: Float!) {
              productById(id: $__fusion_1_id) {
                isExpensive(price: $__fusion_2_price)
                isAvailable
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 201UL);

        // assert
        Assert.True(result.IsEntityLookup);
        Assert.Equal("Product", result.EntityTypeName);
        Assert.Equal("id", result.VariableToKeyFieldMap["__fusion_1_id"]);
        Assert.Equal("price", result.VariableToKeyFieldMap["__fusion_2_price"]);
        Assert.DoesNotContain("price:", result.OperationText);
        Assert.Contains("isExpensive", result.OperationText);
        Assert.Contains("isAvailable", result.OperationText);
    }

    [Fact]
    public void Rewrite_Should_Leave_NonRequireArguments_Untouched()
    {
        // arrange: an argument on a selection field that is not tagged as a
        // require must pass through to the outgoing operation verbatim.
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };
        var entityRequires = new Dictionary<string, EntityRequiresInfo>
        {
            ["Product"] = new EntityRequiresInfo
            {
                Fields = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    ["isExpensive"] = new Dictionary<string, string> { ["price"] = "price" }
                }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields, entityRequires);

        const string sourceText = """
            query($__fusion_1_id: ID!, $__fusion_2_limit: Int!) {
              productById(id: $__fusion_1_id) {
                relatedBy(limit: $__fusion_2_limit)
              }
            }
            """;

        // act
        var result = rewriter.GetOrRewrite(sourceText, 202UL);

        // assert
        Assert.True(result.IsEntityLookup);
        Assert.Contains("limit: $__fusion_2_limit", result.OperationText);
        Assert.False(result.VariableToKeyFieldMap.ContainsKey("__fusion_2_limit"));
    }

    [Fact]
    public void BuildCombinedEntityQuery_Should_Write_RequireField_Into_Representation()
    {
        // arrange: pair the rewritten '@require' variable mapping with a
        // variable payload that carries the bound values. The resulting
        // representations array must contain both the entity key field and
        // the require field projected as top-level entries.
        var lookupFields = new Dictionary<string, LookupFieldInfo>
        {
            ["productById"] = new LookupFieldInfo
            {
                EntityTypeName = "Product",
                ArgumentToKeyFieldMap = new Dictionary<string, string> { ["id"] = "id" }
            }
        };
        var entityRequires = new Dictionary<string, EntityRequiresInfo>
        {
            ["Product"] = new EntityRequiresInfo
            {
                Fields = new Dictionary<string, IReadOnlyDictionary<string, string>>
                {
                    ["isExpensive"] = new Dictionary<string, string> { ["price"] = "price" }
                }
            }
        };
        var rewriter = new FederationQueryRewriter(lookupFields, entityRequires);

        const string sourceText = """
            query($__fusion_1_id: ID!, $__fusion_2_price: Float!) {
              productById(id: $__fusion_1_id) {
                isExpensive(price: $__fusion_2_price)
              }
            }
            """;

        var rewritten = rewriter.GetOrRewrite(sourceText, 203UL);

        var variableValues = CreateVariableValues(
            """{"__fusion_1_id":"p1","__fusion_2_price":9.99}""");

        var requests = ImmutableArray.Create(
            new SourceSchemaClientRequest
            {
                Node = null!,
                SchemaName = "products",
                OperationType = OperationType.Query,
                OperationSourceText = rewritten.OperationText,
                OperationHash = 203UL,
                Variables = [variableValues]
            });

        var rewrittenOps = new[] { rewritten };

        // act
        var (_, variablesJson) =
            ApolloFederationSourceSchemaClient.BuildCombinedEntityQuery(requests, rewrittenOps);

        // assert
        Assert.Contains("\"__typename\":\"Product\"", variablesJson);
        Assert.Contains("\"id\":\"p1\"", variablesJson);
        Assert.Contains("\"price\":9.99", variablesJson);
    }

    private static VariableValues CreateVariableValues(string json)
    {
        var writer = new ChunkedArrayWriter();
        var startPosition = writer.Position;
        using (var jsonWriter = new Utf8JsonWriter(writer))
        {
            using var document = JsonDocument.Parse(json);
            document.RootElement.WriteTo(jsonWriter);
            jsonWriter.Flush();
        }
        var length = writer.Position - startPosition;
        return new VariableValues(default, JsonSegment.Create(writer, startPosition, length));
    }
}
