using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using HotChocolate.Stitching.Schemas.Accounts;
using HotChocolate.Stitching.Schemas.Inventory;
using HotChocolate.Stitching.Schemas.Products;
using HotChocolate.Stitching.Schemas.Reviews;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Squadron;
using StackExchange.Redis;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Stitching.Integration;

public class FederatedRedisSchemaTests
    : IClassFixture<StitchingTestContext>
    , IClassFixture<RedisResource>
{
    private const string _accounts = "accounts";
    private const string _inventory = "inventory";
    private const string _products = "products";
    private const string _reviews = "reviews";

    private readonly ConnectionMultiplexer _connection;

    public FederatedRedisSchemaTests(StitchingTestContext context, RedisResource redisResource)
    {
        Context = context;
        _connection = redisResource.GetConnection();
    }

    private StitchingTestContext Context { get; }

    [Fact]
    public async Task AutoMerge_Schema()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        var configurationName = "C" + Guid.NewGuid().ToString("N");
        var httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

        var database = _connection.GetDatabase();
        while(!cts.IsCancellationRequested)
        {
            if (await database.SetLengthAsync(configurationName) == 4)
            {
                break;
            }

            await Task.Delay(150, cts.Token);
        }

        // act
        var schema =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddQueryType(d => d.Name("Query"))
                .AddRemoteSchemasFromRedis(configurationName, _ => _connection)
                .ModifyOptions(o => o.SortFieldsByName = true)
                .BuildSchemaAsync(cancellationToken: cts.Token);

        // assert
        schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task AutoMerge_HotReload_Schema()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        var configurationName = "C" + Guid.NewGuid().ToString("N");
        var schemaDefinitionV2 = FileResource.Open("AccountSchemaDefinition.json");
        var httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

        var database = _connection.GetDatabase();
        while(!cts.IsCancellationRequested)
        {
            if (await database.SetLengthAsync(configurationName) == 4)
            {
                break;
            }

            await Task.Delay(150, cts.Token);
        }

        var executorResolver =
            new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddQueryType(d => d.Name("Query"))
                .AddRemoteSchemasFromRedis(configurationName, _ => _connection)
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IRequestExecutorResolver>();

        await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);
        var raised = false;

        using var session = executorResolver.Events.Subscribe(
            new RequestExecutorEventObserver(
                args =>
                {
                    if (args.Name.Equals(Schema.DefaultName))
                    {
                        raised = true;
                    }
                }));

        // act
        Assert.False(raised, "eviction was raised before act.");
        await database.StringSetAsync($"{configurationName}.{_accounts}", schemaDefinitionV2);
        await _connection.GetSubscriber().PublishAsync(configurationName, _accounts);

        while(!cts.IsCancellationRequested)
        {
            if (raised)
            {
                break;
            }

            await Task.Delay(150, cts.Token);
        }

        // assert
        Assert.True(raised, "schema evicted.");
        var executor =
            await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);
        var type = executor.Schema.GetType<ObjectType>("User");
        Assert.True(type.Fields.ContainsField("foo"), "foo field exists.");
    }

    [Fact]
    public async Task AutoMerge_HotReload_ClearOperationCaches()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        var configurationName = "C" + Guid.NewGuid().ToString("N");
        var schemaDefinitionV2 = FileResource.Open("AccountSchemaDefinition.json");
        var httpClientFactory = CreateDefaultRemoteSchemas(configurationName);
        var document = Utf8GraphQLParser.Parse("{ foo }");
        var queryHash = "abc";

        var database = _connection.GetDatabase();
        while(!cts.IsCancellationRequested)
        {
            if (await database.SetLengthAsync(configurationName) == 4)
            {
                break;
            }

            await Task.Delay(150, cts.Token);
        }

        var serviceProvider =
            new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .AddQueryType(d => d.Name("Query").Field("foo").Resolve("foo"))
                .AddRemoteSchemasFromRedis(configurationName, _ => _connection)
                .Services
                .BuildServiceProvider();

        var executorResolver =
            serviceProvider.GetRequiredService<IRequestExecutorResolver>();
        var documentCache =
            serviceProvider.GetRequiredService<IDocumentCache>();
        var preparedOperationCache =
            serviceProvider.GetRequiredService<IPreparedOperationCache>();

        await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);
        var raised = false;

        using var session = executorResolver.Events.Subscribe(
            new RequestExecutorEventObserver(
                args =>
                {
                    if (args.Name.Equals(Schema.DefaultName))
                    {
                        raised = true;
                    }
                }));

        Assert.False(documentCache.TryGetDocument(queryHash, out _));
        Assert.False(preparedOperationCache.TryGetOperation(queryHash, out _));

        var requestExecutor =
            await executorResolver.GetRequestExecutorAsync(cancellationToken: cts.Token);

        await requestExecutor
            .ExecuteAsync(QueryRequestBuilder
                    .New()
                    .SetQuery(document)
                    .SetQueryHash(queryHash)
                    .Create(),
                cts.Token);

        Assert.True(preparedOperationCache.TryGetOperation("_Default-1-abc", out _));

        // act
        await database.StringSetAsync($"{configurationName}.{_accounts}", schemaDefinitionV2);
        await _connection.GetSubscriber().PublishAsync(configurationName, _accounts);

        while(!cts.IsCancellationRequested)
        {
            if (raised)
            {
                break;
            }

            await Task.Delay(150, cts.Token);
        }

        // assert
        Assert.True(documentCache.TryGetDocument(queryHash, out _));
        Assert.False(preparedOperationCache.TryGetOperation(queryHash, out _));
    }

    [Fact]
    public async Task AutoMerge_Execute()
    {
        // arrange
        using var cts = new CancellationTokenSource(20_000);
        var configurationName = "C" + Guid.NewGuid().ToString("N");
        var httpClientFactory = CreateDefaultRemoteSchemas(configurationName);

        var database = _connection.GetDatabase();

        while(!cts.IsCancellationRequested)
        {
            if (await database.SetLengthAsync(configurationName) == 4)
            {
                break;
            }

            await Task.Delay(150, cts.Token);
        }

        var executor =
            await new ServiceCollection()
                .AddSingleton(httpClientFactory)
                .AddGraphQL()
                .ModifyOptions(o => o.EnableTag = false)
                .ModifyOptions(o => o.EnableTag = false)
                .AddQueryType(d => d.Name("Query"))
                .AddRemoteSchemasFromRedis(configurationName, _ => _connection)
                .BuildRequestExecutorAsync(cancellationToken: cts.Token);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                me {
                    id
                    name
                    reviews {
                        body
                        product {
                            upc
                        }
                    }
                }
            }
            """,
            cts.Token);

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact(Skip = "Test is flaky")]
    public async Task AutoMerge_AddLocal_Field_Execute()
    {
        await TryTest(async ct =>
        {
            // arrange
            var configurationName = "C" + Guid.NewGuid().ToString("N");
            var httpClientFactory =
                CreateDefaultRemoteSchemas(configurationName);

            var database = _connection.GetDatabase();
            while (!ct.IsCancellationRequested)
            {
                if (await database.SetLengthAsync(configurationName) == 4)
                {
                    break;
                }

                await Task.Delay(150, ct);
            }

            var executor =
                await new ServiceCollection()
                    .AddSingleton(httpClientFactory)
                    .AddGraphQL(configurationName)
                    .ModifyOptions(o => o.EnableTag = false)
                    .AddQueryType(d => d.Name("Query").Field("local").Resolve("I am local."))
                    .AddRemoteSchemasFromRedis(configurationName, _ => _connection)
                    .BuildRequestExecutorAsync(configurationName, ct);

            // act
            var result = await executor.ExecuteAsync(
                """
                {
                    me {
                        id
                        name
                        reviews {
                            body
                            product {
                                upc
                            }
                        }
                    }
                    local
                }
                """,
                ct);

            // assert
            result.ToJson().MatchSnapshot();
        });
    }

    public TestServer CreateAccountsService(string configurationName) =>
        Context.ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .ModifyOptions(o => o.EnableTag = false)
                .AddAccountsSchema()
                .InitializeOnStartup()
                .PublishSchemaDefinition(c => c
                    .SetName(_accounts)
                    .IgnoreRootTypes()
                    .AddTypeExtensionsFromString(
                        """
                        extend type Query {
                            me: User! @delegate(path: "user(id: 1)")
                        }

                        extend type Review {
                            author: User @delegate(path: "user(id: $fields:authorId)")
                        }
                        """)
                    .PublishToRedis(configurationName, _ => _connection)),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

    public TestServer CreateInventoryService(string configurationName) =>
        Context.ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .ModifyOptions(o => o.EnableTag = false)
                .AddInventorySchema()
                .InitializeOnStartup()
                .PublishSchemaDefinition(c => c
                    .SetName(_inventory)
                    .IgnoreRootTypes()
                    .AddTypeExtensionsFromString(
                        """
                        extend type Product {
                            inStock: Boolean
                                @delegate(path: "inventoryInfo(upc: $fields:upc).isInStock")

                            shippingEstimate: Int
                                @delegate(path: "shippingEstimate(price: $fields:price weight: $fields:weight)")
                        }
                        """)
                    .PublishToRedis(configurationName, _ => _connection)),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

    public TestServer CreateProductsService(string configurationName) =>
        Context.ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .ModifyOptions(o => o.EnableTag = false)
                .AddProductsSchema()
                .InitializeOnStartup()
                .PublishSchemaDefinition(c => c
                    .SetName(_products)
                    .IgnoreRootTypes()
                    .AddTypeExtensionsFromString(
                        """
                        extend type Query {
                            topProducts(first: Int = 5): [Product] @delegate
                        }

                        extend type Review {
                            product: Product @delegate(path: "product(upc: $fields:upc)")
                        }
                        """)
                    .PublishToRedis(configurationName, _ => _connection)),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

    public TestServer CreateReviewsService(string configurationName) =>
        Context.ServerFactory.Create(
            services => services
                .AddRouting()
                .AddGraphQLServer()
                .ModifyOptions(o => o.EnableTag = false)
                .AddReviewSchema()
                .InitializeOnStartup()
                .PublishSchemaDefinition(c => c
                    .SetName(_reviews)
                    .IgnoreRootTypes()
                    .AddTypeExtensionsFromString(
                        """
                        extend type User {
                            reviews: [Review]
                                @delegate(path:"reviewsByAuthor(authorId: $fields:id)")
                        }

                        extend type Product {
                            reviews: [Review]
                                @delegate(path:"reviewsByProduct(upc: $fields:upc)")
                        }
                        """)
                    .PublishToRedis(configurationName, _ => _connection)),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL("/")));

    public IHttpClientFactory CreateDefaultRemoteSchemas(string configurationName)
    {
        var connections = new Dictionary<string, HttpClient>
        {
            { _accounts, CreateAccountsService(configurationName).CreateClient() },
            { _inventory, CreateInventoryService(configurationName).CreateClient() },
            { _products, CreateProductsService(configurationName).CreateClient() },
            { _reviews, CreateReviewsService(configurationName).CreateClient() },
        };

        return StitchingTestContext.CreateRemoteSchemas(connections);
    }
}
