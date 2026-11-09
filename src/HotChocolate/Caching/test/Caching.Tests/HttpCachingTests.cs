using System.Net.Http.Headers;
using System.Text;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Http.Tests;

public class HttpCachingTests : ServerTestBase
{
    public HttpCachingTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task MaxAge_NonZero_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                    .Field("field")
                    .Resolve("")
                    .CacheControl(2000));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxAge_Zero_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                    .Field("field")
                    .Resolve("")
                    .CacheControl(0));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Theory]
    [InlineData(60, 30)]
    [InlineData(30, 60)]
    [InlineData(30, 30)]
    [InlineData(30, 3000)]
    public async Task MaxAge_Multiple_Should_Cache_Shortest_Time(int time1, int time2)
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(time1);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(time2);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxAge_Multiple_Combine_Public_Private_Caches_Private()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(30);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(60, CacheControlScope.Private);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAge_Multiple_Combine_Public_Private_Caches_Private()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(sharedMaxAge:60);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(scope:CacheControlScope.Private, sharedMaxAge:30);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAge_MaxAge_Combine_Produces_Resolved_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(maxAge: 0, sharedMaxAge:60);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(maxAge:30);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxAge_SharedMaxAge_Combine_Produces_Resolved_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(maxAge: 30);
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(maxAge: 0, sharedMaxAge:60);
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Just_Defaults_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve(""));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task No_Applied_Defaults_Should_Not_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve(""));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Default_Max_Age_Should_Apply_And_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.DefaultMaxAge = 1000)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve(""));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task Default_Scope_Should_Apply_And_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.DefaultScope = CacheControlScope.Private)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve(""));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task JustScope_Should_Not_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                    .Field("field")
                    .Resolve("")
                    .CacheControl(scope: CacheControlScope.Private));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task MaxAgeAndScope_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                    .Field("field")
                    .Resolve("")
                    .CacheControl(2000, CacheControlScope.Private));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task QueryError_Should_Not_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Type<StringType>()
                        .Resolve(_ => throw new Exception()));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAgeAndScope_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 2000, scope: CacheControlScope.Public));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAgeAndVary_Should_Cache()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                    d.Name("Query")
                        .Field("field")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 2000, vary: new[] { "X-foo", "X-BaR" }));
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field }");

        result.MatchSnapshot();
    }

    [Fact]
    public async Task SharedMaxAgeAndVary_Multiple_Should_Cache_And_Combine()
    {
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType(d =>
                {
                    var o = d.Name("Query");
                    o.Field("field1")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 2000, vary: new[] {"X-foo", "X-BaR"});
                    o.Field("field2")
                        .Resolve("")
                        .CacheControl(sharedMaxAge: 1000, vary: new[] {"X-FAR", "X-BaR"});
                });
        });

        var client = server.CreateClient();
        var result = await client.PostQueryAsync("{ field1, field2 }");

        result.MatchSnapshot();
    }

    [Theory]
    [InlineData(
        """{ node(id: "UHJvZHVjdDox") { ... on Product { name } } }""",
        """{"data":{"node":{"name":"Product 1"}}}""")]
    [InlineData(
        """{ nodes(ids: ["UHJvZHVjdDox"]) { ... on Product { name } } }""",
        """{"data":{"nodes":[{"name":"Product 1"}]}}""")]
    public async Task NodeFields_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled(
        string query,
        string expectedBody)
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddGlobalObjectIdentification()
                .AddQueryType<ProductQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync(query);

        // assert
        Assert.Equal(expectedBody, result.Body);
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task EntitiesField_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddApolloFederation()
                .AddQueryType<ReviewQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync(
            """
            {
                _entities(representations: [{ __typename: "Review", id: 1 }]) {
                    ... on Review {
                        body
                    }
                }
            }
            """);

        // assert
        Assert.Equal("""{"data":{"_entities":[{"body":"Review 1"}]}}""", result.Body);
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task ServiceField_Should_UseDefaultMaxAge_When_ApplyDefaultsIsEnabled()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddApolloFederation()
                .AddQueryType<ReviewQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ _service { sdl } }");

        // assert
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task PagingField_Should_UseDefaultMaxAge_When_ResolverIsSynchronous()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<CatalogQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ catalog { tags { nodes } } }");

        // assert
        Assert.Equal("""{"data":{"catalog":{"tags":{"nodes":["new"]}}}}""", result.Body);
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RootField_Should_UseDefaultMaxAge_When_CacheControlDeclaresOnlyScope()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<StoreQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ name profile }");

        // assert
        Assert.Equal("""{"data":{"name":"Main","profile":"profile"}}""", result.Body);
        Assert.Equal("max-age=0, private", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RootField_Should_UseReturnTypeMaxAge_When_CacheControlDeclaresOnlyScope()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<ViewerQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ me { email } }");

        // assert
        Assert.Equal("""{"data":{"me":{"email":"alice@example.com"}}}""", result.Body);
        Assert.Equal("max-age=60, private", result.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData(
        "{ owner { email } }",
        """{"data":{"owner":{"email":"alice@example.com"}}}""")]
    [InlineData(
        "{ members { email } }",
        """{"data":{"members":[{"email":"alice@example.com"}]}}""")]
    public async Task RootField_Should_UseReturnTypeMaxAge_When_ScopeOnlyFieldTypeIsNonNullOrList(
        string query,
        string expectedBody)
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<TeamQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync(query);

        // assert
        Assert.Equal(expectedBody, result.Body);
        Assert.Equal("max-age=60, private", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RootField_Should_UseLowestObjectTypeMaxAge_When_ScopeOnlyFieldTypeIsUnion()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<PublicationQuery>()
                .AddType<Book>()
                .AddType<Magazine>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ saved { __typename } }");

        // assert
        Assert.Equal("""{"data":{"saved":{"__typename":"Magazine"}}}""", result.Body);
        Assert.Equal("max-age=30, private", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RootField_Should_UseDefaultMaxAge_When_ScopeOnlyUnionHasMemberWithoutMaxAge()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<FeedQuery>()
                .AddType<Announcement>()
                .AddType<Event>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ feed { __typename } }");

        // assert
        Assert.Equal("""{"data":{"feed":{"__typename":"Announcement"}}}""", result.Body);
        Assert.Equal("max-age=0, private", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task RootField_Should_KeepPublicScope_When_DefaultScopeIsPrivate()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.DefaultScope = CacheControlScope.Private)
                .AddQueryType<PublicScopeQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ name motto }");

        // assert
        Assert.Equal("""{"data":{"name":"Main","motto":"Always open"}}""", result.Body);
        Assert.Equal("max-age=0", result.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData(
        "{ requiredAccount { email } }",
        """{"data":{"requiredAccount":{"email":"alice@example.com"}}}""")]
    [InlineData(
        "{ accounts { email } }",
        """{"data":{"accounts":[{"email":"alice@example.com"}]}}""")]
    [InlineData(
        "{ post { author { email } } }",
        """{"data":{"post":{"author":{"email":"alice@example.com"}}}}""")]
    public async Task Field_Should_UseReturnTypeScope_When_TypeIsNonNullOrList(
        string query,
        string expectedBody)
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<AccountQuery>()
                .AddType<Article>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync(query);

        // assert
        Assert.Equal(expectedBody, result.Body);
        Assert.Equal("max-age=60, private", result.Headers.CacheControl?.ToString());
    }

    [Theory]
    [InlineData(
        "{ search { ... on Account { email } } }",
        """{"data":{"search":{"email":"alice@example.com"}}}""")]
    [InlineData(
        "{ search { ... on Article { title } } }",
        """{"data":{"search":{}}}""")]
    [InlineData(
        "{ contact { email } }",
        """{"data":{"contact":{"email":"alice@example.com"}}}""")]
    public async Task Field_Should_UseObjectTypeScope_When_TypeIsUnionOrInterface(
        string query,
        string expectedBody)
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .AddQueryType<AccountQuery>()
                .AddType<Article>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync(query);

        // assert
        Assert.Equal(expectedBody, result.Body);
        Assert.Equal("max-age=60, private", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Field_Should_UseReturnTypeMaxAgeAndVary_When_TypeIsNonNull()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType<SettingsQuery>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ settings { theme } }");

        // assert
        Assert.Equal("""{"data":{"settings":{"theme":"dark"}}}""", result.Body);
        Assert.Equal("max-age=10, s-maxage=5", result.Headers.CacheControl?.ToString());
        Assert.Equal(["authorization"], result.Headers.Vary);
    }

    [Fact]
    public async Task Field_Should_UseLowestObjectTypeMaxAge_When_UnionFieldSetsNoMaxAge()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType<PublicationQuery>()
                .AddType<Book>()
                .AddType<Magazine>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ latest { __typename } }");

        // assert
        Assert.Equal("""{"data":{"latest":{"__typename":"Magazine"}}}""", result.Body);
        Assert.Equal("max-age=30", result.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Field_Should_KeepItsMaxAge_When_UnionObjectTypeSetsLowerMaxAge()
    {
        // arrange
        var server = CreateServer(services =>
        {
            services.AddGraphQLServer()
                .UseQueryCachePipeline()
                .AddCacheControl()
                .ModifyCacheControlOptions(o => o.ApplyDefaults = false)
                .AddQueryType<PublicationQuery>()
                .AddType<Book>()
                .AddType<Magazine>();
        });
        var client = server.CreateClient();

        // act
        var result = await client.GetQueryAsync("{ featured { __typename } }");

        // assert
        Assert.Equal("""{"data":{"featured":{"__typename":"Magazine"}}}""", result.Body);
        Assert.Equal("max-age=120", result.Headers.CacheControl?.ToString());
    }

    [CacheControl(Scope = CacheControlScope.Private)]
    public record Account(int Id, string Email) : IContact, ISearchResult;

    public class AccountQuery
    {
        [CacheControl(60)]
        public List<Account> GetAccounts() => [new(1, "alice@example.com")];

        [CacheControl(60)]
        public IContact GetContact() => new Account(1, "alice@example.com");

        [CacheControl(60)]
        public Post GetPost() => new(new Account(1, "alice@example.com"));

        [CacheControl(60)]
        public Account GetRequiredAccount() => new(1, "alice@example.com");

        [CacheControl(60)]
        public ISearchResult GetSearch() => new Account(1, "alice@example.com");
    }

    public record Announcement(string Title) : IFeedItem;

    public record Article(string Title) : ISearchResult;

    [CacheControl(30)]
    public record Book(string Title) : IPublication;

    public class Catalog
    {
        [UsePaging]
        public List<string> GetTags() => ["new"];
    }

    public class CatalogQuery
    {
        [CacheControl(3600)]
        public Catalog GetCatalog() => new();
    }

    [CacheControl(30)]
    public record Event(string Title) : IFeedItem;

    public class FeedQuery
    {
        [CacheControl(Scope = CacheControlScope.Private)]
        public IFeedItem GetFeed() => new Announcement("Closed today");
    }

    [InterfaceType("Contact")]
    public interface IContact
    {
        string Email { get; }
    }

    [UnionType("FeedItem")]
    public interface IFeedItem;

    [UnionType("Publication")]
    public interface IPublication;

    [UnionType("SearchResult")]
    public interface ISearchResult;

    [CacheControl(60)]
    public record Magazine(string Title) : IPublication;

    public record Post(Account Author);

    [Node]
    public class Product
    {
        public int Id { get; init; }

        [CacheControl(3600)]
        public string Name { get; init; } = default!;

        [NodeResolver]
        public static Product Get(int id) => new() { Id = id, Name = $"Product {id}" };
    }

    public class ProductQuery
    {
        public Product GetProduct(int id) => Product.Get(id);
    }

    public class PublicationQuery
    {
        [CacheControl(120)]
        public IPublication GetFeatured() => new Magazine("Monthly");

        public IPublication GetLatest() => new Magazine("Monthly");

        [CacheControl(Scope = CacheControlScope.Private)]
        public IPublication GetSaved() => new Magazine("Monthly");
    }

    public class PublicScopeQuery
    {
        [CacheControl(Scope = CacheControlScope.Public)]
        public string GetMotto() => "Always open";

        [CacheControl(3600)]
        public string GetName() => "Main";
    }

    [Key("id")]
    public class Review
    {
        public int Id { get; init; }

        [CacheControl(3600)]
        public string Body { get; init; } = default!;

        [ReferenceResolver]
        public static Review Get(int id) => new() { Id = id, Body = $"Review {id}" };
    }

    public class ReviewQuery
    {
        public Review GetReview(int id) => Review.Get(id);
    }

    [CacheControl(10, SharedMaxAge = 5, Vary = ["Authorization"])]
    public record Settings(string Theme);

    public class SettingsQuery
    {
        public Settings GetSettings() => new("dark");
    }

    public class StoreQuery
    {
        [CacheControl(3600)]
        public string GetName() => "Main";

        [CacheControl(Scope = CacheControlScope.Private)]
        public string GetProfile() => "profile";
    }

    public class TeamQuery
    {
        [CacheControl(Scope = CacheControlScope.Private)]
        public List<User> GetMembers() => [new()];

        [CacheControl(Scope = CacheControlScope.Private)]
        public User GetOwner() => new();
    }

    [CacheControl(60)]
    public class User
    {
        public string Email => "alice@example.com";
    }

    public class ViewerQuery
    {
        [CacheControl(Scope = CacheControlScope.Private)]
        public User? GetMe() => new();
    }
}

public class GraphQLResult
{
    public HttpResponseHeaders Headers { get; set; } = default!;

    public HttpContentHeaders ContentHeaders { get; set; } = default!;

    public string Body { get; set; } = default!;
}

internal static class TestServerExtensions
{
    public static async Task<GraphQLResult> GetQueryAsync(this HttpClient client, string query)
    {
        var response = await client.GetAsync("/graphql?query=" + Uri.EscapeDataString(query));

        return new GraphQLResult
        {
            Headers = response.Headers,
            ContentHeaders = response.Content.Headers,
            Body = await response.Content.ReadAsStringAsync()
        };
    }

    public static async Task<GraphQLResult> PostQueryAsync(this HttpClient client, string query)
    {
        var payload = $"{{ \"query\": \"{query}\" }}";

        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/graphql", content);

        var result = new GraphQLResult
        {
            Headers = response.Headers,
            ContentHeaders = response.Content.Headers,
            Body = await response.Content.ReadAsStringAsync(),
        };

        return result;
    }
}
