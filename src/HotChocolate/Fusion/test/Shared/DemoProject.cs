using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Shared.Accounts;
using HotChocolate.Fusion.Shared.Products;
using HotChocolate.Fusion.Shared.Reviews;
using HotChocolate.Utilities.Introspection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Shared;

public sealed class DemoProject : IDisposable
{
    private readonly IReadOnlyList<IDisposable> _disposables;
    private bool _disposed;

    private DemoProject(
        IReadOnlyList<IDisposable> disposables,
        DemoSubgraph accounts,
        DemoSubgraph reviews,
        DemoSubgraph products,
        IHttpClientFactory clientFactory,
        IWebSocketConnectionFactory webSocketConnectionFactory)
    {
        _disposables = disposables;
        Accounts = accounts;
        Reviews = reviews;
        Products = products;
        HttpClientFactory = clientFactory;
        WebSocketConnectionFactory = webSocketConnectionFactory;
    }

    public IHttpClientFactory HttpClientFactory { get; }

    public IWebSocketConnectionFactory WebSocketConnectionFactory { get; }

    public DemoSubgraph Reviews { get; }

    public DemoSubgraph Products { get; }

    public DemoSubgraph Accounts { get; }

    public static async Task<DemoProject> CreateAsync(CancellationToken ct = default)
    {
        var disposables = new List<IDisposable>();
        TestServerFactory testServerFactory = new();
        disposables.Add(testServerFactory);

        var introspection = new IntrospectionClient();

        var reviews = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ReviewRepository>()
                .AddGraphQLServer()
                .AddQueryType<ReviewsQuery>()
                .AddMutationType<ReviewsMutation>()
                .AddSubscriptionType<ReviewsSubscription>()
                .AddMutationConventions(),
            c => c
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(reviews);

        var reviewsClient = reviews.CreateClient();
        reviewsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var reviewsSchema = await introspection
            .DownloadSchemaAsync(reviewsClient, ct)
            .ConfigureAwait(false);

        var accounts = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<UserRepository>()
                .AddGraphQLServer()
                .AddQueryType<AccountQuery>()
                .AddMutationType<AccountMutation>()
                .AddMutationConventions(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(accounts);

        var accountsClient = accounts.CreateClient();
        accountsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var accountsSchema = await introspection
            .DownloadSchemaAsync(accountsClient, ct)
            .ConfigureAwait(false);

        var products = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ProductRepository>()
                .AddGraphQLServer()
                .AddQueryType<ProductQuery>(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(products);

        var productsClient = products.CreateClient();
        productsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var productsSchema = await introspection
            .DownloadSchemaAsync(productsClient, ct)
            .ConfigureAwait(false);

        var httpClients = new Dictionary<string, Func<HttpClient>>
        {
            {
                "Reviews", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = reviews.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
            {
                "Accounts", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = accounts.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
            {
                "Products", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = products.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
        };

        var webSocketClients = new Dictionary<string, Func<IWebSocketConnection>>
        {
            {
                "Reviews", () => new MockWebSocketConnection(reviews.CreateWebSocketClient())
            },
            {
                "Accounts", () => new MockWebSocketConnection(accounts.CreateWebSocketClient())
            },
            {
                "Products", () => new MockWebSocketConnection(products.CreateWebSocketClient())
            },
        };

        return new DemoProject(
            disposables,
            new DemoSubgraph(
                "Accounts",
                accountsClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                accountsSchema,
                accounts),
            new DemoSubgraph(
                "Reviews",
                reviewsClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                reviewsSchema,
                reviews),
            new DemoSubgraph(
                "Products",
                productsClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                productsSchema,
                products),
            new MockHttpClientFactory(httpClients),
            new MockWebSocketConnectionFactory(webSocketClients));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposed = true;
        }
    }
}
