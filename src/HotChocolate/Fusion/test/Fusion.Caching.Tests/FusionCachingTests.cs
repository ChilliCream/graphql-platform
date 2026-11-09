using System.Text;
using HotChocolate.Caching;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;
using CacheControlAttribute = HotChocolate.Caching.CacheControlAttribute;

namespace HotChocolate.Fusion.Caching;

public partial class FusionCachingTests : FusionTestBase
{
    [Fact]
    public async Task Query_With_CacheControl_MaxAge_Should_Return_CacheControl_Header()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ expensiveField }");

        // assert
        Assert.NotNull(response.Headers.CacheControl);
        Assert.Equal(TimeSpan.FromSeconds(3600), response.Headers.CacheControl.MaxAge);
    }

    [Fact]
    public async Task Query_With_Multiple_Fields_Should_Return_Shortest_MaxAge()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ expensiveField cheaperField }");

        // assert
        Assert.NotNull(response.Headers.CacheControl);
        Assert.Equal(TimeSpan.FromSeconds(1800), response.Headers.CacheControl.MaxAge);
    }

    [Fact]
    public async Task Query_With_Private_Scope_Should_Return_Private_Header()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<PrivateCachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ userData }");

        // assert
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl.Private);
        Assert.Equal(TimeSpan.FromSeconds(3600), response.Headers.CacheControl.MaxAge);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("QUERY")]
    public async Task Query_With_SharedMaxAge_Should_Return_SMaxAge_Header(string method)
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<SharedCachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .ModifyServerOptions(o => o.EnableQueryRequests = true)
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.SendGraphQLAsync(new HttpMethod(method), "{ sharedData }");

        // assert
        Assert.NotNull(response.Headers.CacheControl);
        Assert.Equal(TimeSpan.FromSeconds(1800), response.Headers.CacheControl.SharedMaxAge);
    }

    [Fact]
    public async Task Mutation_Should_Not_Return_CacheControl_Header()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<MutationSchema.Query>()
                .AddMutationType<MutationSchema.Mutation>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("mutation { doSomething }");

        // assert
        Assert.Null(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Query_With_No_CacheControl_Directive_Should_Not_Cache()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<NoCacheSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ plainField }");

        // assert
        Assert.Null(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Query_With_Error_Should_Not_Cache()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<ErrorSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ errorField }");

        // assert
        Assert.Null(response.Headers.CacheControl);
    }

    [Fact]
    public async Task Interceptor_Should_Compute_Constraints_From_Composite_Schema()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        var verifyingInterceptor = new VerifyingInterceptor();

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .AddOperationPlannerInterceptor(_ => verifyingInterceptor)
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ expensiveField }");

        // assert
        Assert.True(verifyingInterceptor.HasHitOnAfterPlanCompleted);
        Assert.True(verifyingInterceptor.HasCacheConstraints);
        Assert.True(verifyingInterceptor.HasCacheControlHeaderValue);
    }

    [Theory]
    [InlineData("{ plainField }")]
    [InlineData("{ expensiveField plainField }")]
    [InlineData("{ plainField expensiveField }")]
    public async Task Query_Should_UseDefaultMaxAge_When_RootFieldHasNoCacheControl(string query)
    {
        // arrange
        using var cachedServer = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var plainServer = CreateSourceSchema(
            "b",
            b => b.AddQueryType<NoCacheSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", cachedServer), ("b", plainServer)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync(query);

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseConfiguredDefaults_When_RootFieldHasNoCacheControl()
    {
        // arrange
        using var cachedServer = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var plainServer = CreateSourceSchema(
            "b",
            b => b.AddQueryType<NoCacheSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", cachedServer), ("b", plainServer)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .ModifyCacheControlOptions(o =>
                {
                    o.DefaultMaxAge = 60;
                    o.DefaultScope = CacheControlScope.Private;
                })
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ expensiveField plainField }");

        // assert
        Assert.Equal("max-age=60, private", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_RootFieldDeclaresOnlyScope()
    {
        // arrange
        using var cachedServer = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var profileServer = CreateSourceSchema(
            "b",
            b => b.AddQueryType<ScopeOnlySchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", cachedServer), ("b", profileServer)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ expensiveField profile }");

        // assert
        Assert.Equal("max-age=0, private", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseReturnTypeMaxAge_When_RootFieldDeclaresOnlyScope()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<ViewerSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ me { email } }");

        // assert
        Assert.Equal("max-age=60, private", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_ShareableRootFieldIsCacheableInOneSchema()
    {
        // arrange
        using var cachedServer = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedShareableSchema.Query>()
                .AddCacheControl());

        using var plainServer = CreateSourceSchema(
            "b",
            b => b.AddQueryType<ShareableSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", cachedServer), ("b", plainServer)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ announcement }");

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_KeepMaxAge_When_TypenameIsSelected()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<CachedSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", server)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.PostGraphQLAsync("{ __typename expensiveField }");

        // assert
        Assert.Equal("max-age=3600", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_LookupBringsInFieldWithoutCacheControl()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<WishlistSchema.Query>());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { name inWishlist } }");

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_KeepMaxAge_When_NoLookupIsNeeded()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<WishlistSchema.Query>());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { name } }");

        // assert
        Assert.Equal("max-age=300", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_LookupsShareABatch()
    {
        // arrange
        using var catalogServer = CreateSourceSchema(
            "catalog",
            b => b.AddQueryType<LookupCatalogSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var reviewsServer = CreateSourceSchema(
            "reviews",
            b => b.AddQueryType<ReviewsSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var wishlistServer = CreateSourceSchema(
            "wishlist",
            b => b.AddQueryType<WishlistSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("catalog", catalogServer),
                ("reviews", reviewsServer),
                ("wishlist", wishlistServer)
            ],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync(
            "{ products { inWishlist } topReviews { product { inWishlist } } }");

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_KeepLookupFieldMaxAge_When_FieldDeclaresMaxAge()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<MaxAgeWishlistSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { inWishlist } }");

        // assert
        Assert.Equal("max-age=60", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseReturnTypeMaxAge_When_LookupFieldDeclaresOnlyScope()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<DetailedWishlistSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { owner { email } } }");

        // assert
        Assert.Equal("max-age=60, private", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_LookupFieldDeclaresOnlyScope()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<DetailedWishlistSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { note } }");

        // assert
        Assert.Equal("max-age=0, private", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_InheritMaxAge_When_LookupFieldInheritsMaxAge()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<InheritingWishlistSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { inWishlist } }");

        // assert
        Assert.Equal("max-age=300", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_KeepMaxAge_When_LookupFieldChildrenDeclareNothing()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<DetailedWishlistSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync(
            "{ products { wishlist { items { name } } } }");

        // assert
        Assert.Equal("max-age=60", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_LookupFieldIsAliased()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<WishlistSchema.Query>());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { saved: inWishlist } }");

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_DeferOfLookupFieldIsDisabled()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<WishlistSchema.Query>());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync(
            "query($defer: Boolean!) { products { name ... @defer(if: $defer) { inWishlist } } }",
            """{"defer":false}""");

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_KeepLookupFieldMaxAge_When_TypenameIsSelected()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<MaxAgeWishlistSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync(
            "{ products { __typename inWishlist } }");

        // assert
        Assert.Equal("max-age=60", response.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData("{ products { name } }", "max-age=300")]
    [InlineData("{ topReviews { product { name } } }", "max-age=0")]
    public async Task Query_Should_DependOnPlan_When_ShareableFieldIsSelected(
        string query,
        string expectedCacheControl)
    {
        // arrange
        using var catalogServer = CreateSourceSchema(
            "catalog",
            b => b.AddQueryType<ShareableCatalogSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var reviewsServer = CreateSourceSchema(
            "reviews",
            b => b.AddQueryType<ReviewsSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var wishlistServer = CreateSourceSchema(
            "wishlist",
            b => b.AddQueryType<ShareableWishlistSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("catalog", catalogServer),
                ("reviews", reviewsServer),
                ("wishlist", wishlistServer)
            ],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync(query);

        // assert
        Assert.Equal(expectedCacheControl, response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_IgnoreLookups_When_ApplyDefaultsIsDisabled()
    {
        // arrange
        using var gateway = await CreateWishlistGatewayAsync(
            b => b.AddQueryType<WishlistSchema.Query>(),
            o => o.ApplyDefaults = false);

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync("{ products { name inWishlist } }");

        // assert
        Assert.Equal("max-age=300", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Query_Should_UseDefaultMaxAge_When_LookupFillsUnionMember()
    {
        // arrange
        using var catalogServer = CreateSourceSchema(
            "catalog",
            b => b.AddQueryType<SearchCatalogSchema.Query>()
                .AddType<SearchCatalogSchema.Brand>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        using var wishlistServer = CreateSourceSchema(
            "wishlist",
            b => b.AddQueryType<WishlistSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("catalog", catalogServer), ("wishlist", wishlistServer)],
            configureGatewayBuilder: b => b
                .AddCacheControl()
                .UseQueryCache());

        // act
        var httpClient = gateway.CreateClient();
        var response = await httpClient.GetGraphQLAsync(
            "{ search { ... on Product { inWishlist } ... on Brand { name } } }");

        // assert
        Assert.Equal("max-age=0", response.Headers.CacheControl?.ToString());
    }

    private async Task<Gateway> CreateWishlistGatewayAsync(
        Action<IRequestExecutorBuilder> configureWishlist,
        Action<CacheControlOptions>? modifyGatewayOptions = null)
    {
        var catalogServer = CreateSourceSchema(
            "catalog",
            b => b.AddQueryType<LookupCatalogSchema.Query>()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false));

        var wishlistServer = CreateSourceSchema("wishlist", configureWishlist);

        return await CreateCompositeSchemaAsync(
            [("catalog", catalogServer), ("wishlist", wishlistServer)],
            configureGatewayBuilder: b =>
            {
                b.AddCacheControl().UseQueryCache();

                if (modifyGatewayOptions is not null)
                {
                    b.ModifyCacheControlOptions(modifyGatewayOptions);
                }
            });
    }

    private sealed class VerifyingInterceptor : IOperationPlannerInterceptor
    {
        public bool HasHitOnAfterPlanCompleted;
        public bool HasCacheConstraints;
        public bool HasCacheControlHeaderValue;

        public void OnAfterPlanCompleted(
            OperationDocumentInfo operationDocumentInfo,
            OperationPlan operationPlan)
        {
            HasHitOnAfterPlanCompleted = true;
            HasCacheConstraints = operationPlan.Operation.Features
                .TryGet<ImmutableCacheConstraints>(out _);
            HasCacheControlHeaderValue = operationPlan.Operation.Features
                .TryGet<Microsoft.Net.Http.Headers.CacheControlHeaderValue>(out _);
        }
    }
}

public static class CachedSchema
{
    public class Query
    {
        [CacheControl(3600)]
        public string ExpensiveField() => "expensive";

        [CacheControl(1800)]
        public string CheaperField() => "cheaper";
    }
}

public static class PrivateCachedSchema
{
    public class Query
    {
        [CacheControl(3600, Scope = CacheControlScope.Private)]
        public string UserData() => "private-data";
    }
}

public static class SharedCachedSchema
{
    public class Query
    {
        [CacheControl(SharedMaxAge = 1800)]
        public string SharedData() => "shared";
    }
}

public static class NoCacheSchema
{
    public class Query
    {
        public string PlainField() => "plain";
    }
}

public static class ErrorSchema
{
    public class Query
    {
        [CacheControl(3600)]
        public string ErrorField() => throw new Exception("Boom!");
    }
}

public static class MutationSchema
{
    public class Query
    {
        public string Hello() => "world";
    }

    public class Mutation
    {
        public string DoSomething() => "done";
    }
}

public static class ScopeOnlySchema
{
    public class Query
    {
        [CacheControl(Scope = CacheControlScope.Private)]
        public string Profile() => "profile";
    }
}

public static class ViewerSchema
{
    public class Query
    {
        [CacheControl(Scope = CacheControlScope.Private)]
        public User? GetMe() => new();
    }

    [CacheControl(60)]
    public class User
    {
        public string Email => "alice@example.com";
    }
}

public static class CachedShareableSchema
{
    public class Query
    {
        [Shareable]
        [CacheControl(300)]
        public string Announcement() => "announcement";
    }
}

public static class ShareableSchema
{
    public class Query
    {
        [Shareable]
        public string Announcement() => "announcement";
    }
}

public static class LookupCatalogSchema
{
    public class Query
    {
        [CacheControl(300)]
        public List<Product> GetProducts() => [new(1, "Chair")];

        [Lookup]
        [Internal]
        public Product? GetProductById(int id) => new(id, "Chair");
    }

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id, string Name);
}

public static class DetailedWishlistSchema
{
    public class Query
    {
        [Lookup]
        [Internal]
        public Product? GetProductById(int id) => new(id);
    }

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id)
    {
        [CacheControl(Scope = CacheControlScope.Private)]
        public string Note => "note";

        [CacheControl(Scope = CacheControlScope.Private)]
        public User? Owner => new("alice@example.com");

        [CacheControl(60)]
        public Wishlist Wishlist => new([new WishlistItem("Lamp")]);
    }

    [CacheControl(60)]
    public record User(string Email);

    public record Wishlist(List<WishlistItem> Items);

    public record WishlistItem(string Name);
}

public static class InheritingWishlistSchema
{
    public class Query
    {
        [Lookup]
        [Internal]
        public Product? GetProductById(int id) => new(id);
    }

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id)
    {
        [CacheControl(InheritMaxAge = true)]
        public bool InWishlist => true;
    }
}

public static class MaxAgeWishlistSchema
{
    public class Query
    {
        [Lookup]
        [Internal]
        public Product? GetProductById(int id) => new(id);
    }

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id)
    {
        [CacheControl(60)]
        public bool InWishlist => true;
    }
}

public static class ReviewsSchema
{
    public class Query
    {
        [CacheControl(300)]
        public List<Review> GetTopReviews() => [new("Great chair", new Product(1))];
    }

    public record Review(string Body, Product Product);

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id);
}

public static class ShareableCatalogSchema
{
    public class Query
    {
        [CacheControl(300)]
        public List<Product> GetProducts() => [new(1)];

        [Lookup]
        [Internal]
        public Product? GetProductById(int id) => new(id);
    }

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id)
    {
        [Shareable]
        public string Name => "Chair";
    }
}

public static class SearchCatalogSchema
{
    public class Query
    {
        [CacheControl(300)]
        public List<ISearchResult> GetSearch() => [new Product(1, "Chair"), new Brand("Oak")];

        [Lookup]
        [Internal]
        public Product? GetProductById(int id) => new(id, "Chair");
    }

    [HotChocolate.Types.UnionType("SearchResult")]
    public interface ISearchResult;

    [GraphQLName("Brand")]
    public record Brand(string Name) : ISearchResult;

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id, string Name) : ISearchResult;
}

public static class ShareableWishlistSchema
{
    public class Query
    {
        [Lookup]
        [Internal]
        public Product? GetProductById(int id) => new(id);
    }

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id)
    {
        [Shareable]
        public string Name => "Chair";
    }
}

public static class WishlistSchema
{
    public class Query
    {
        [Lookup]
        [Internal]
        public Product? GetProductById(int id) => new(id);
    }

    [EntityKey("id")]
    [GraphQLName("Product")]
    public record Product(int Id)
    {
        public bool InWishlist => true;
    }
}

internal static class TestHttpClientExtensions
{
    public static Task<HttpResponseMessage> GetGraphQLAsync(this HttpClient client, string query)
        => client.GetAsync("/graphql?query=" + Uri.EscapeDataString(query));

    public static Task<HttpResponseMessage> GetGraphQLAsync(
        this HttpClient client,
        string query,
        string variables)
        => client.GetAsync(
            "/graphql?query=" + Uri.EscapeDataString(query)
            + "&variables=" + Uri.EscapeDataString(variables));

    public static Task<HttpResponseMessage> PostGraphQLAsync(this HttpClient client, string query)
        => client.SendGraphQLAsync(HttpMethod.Post, query);

    public static async Task<HttpResponseMessage> SendGraphQLAsync(
        this HttpClient client,
        HttpMethod method,
        string query)
    {
        var payload = $$"""{"query":"{{query}}"}""";
        using var request = new HttpRequestMessage(method, "/graphql")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        return await client.SendAsync(request);
    }
}
