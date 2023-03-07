using HotChocolate.AspNetCore.Tests.Utilities;
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
    private readonly IHttpClientFactory _clientFactory;
    private bool _disposed;

    private DemoProject(
        IReadOnlyList<IDisposable> disposables,
        DemoSubgraph accounts,
        DemoSubgraph reviews,
        DemoSubgraph products,
        IHttpClientFactory clientFactory)
    {
        _disposables = disposables;
        Accounts = accounts;
        Reviews = reviews;
        Products = products;
        _clientFactory = clientFactory;
    }

    public IHttpClientFactory HttpClientFactory => _clientFactory;

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
                .AddQueryType<ReviewQuery>(),
            c => c
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
                .AddQueryType<AccountQuery>(),
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


        var clients = new Dictionary<string, Func<HttpClient>>
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

        return new DemoProject(
            disposables,
            new DemoSubgraph("Accounts", accountsClient.BaseAddress, accountsSchema, accounts),
            new DemoSubgraph("Reviews", reviewsClient.BaseAddress, reviewsSchema, reviews),
            new DemoSubgraph("Products", productsClient.BaseAddress, productsSchema, products),
            new MockHttpClientFactory(clients));
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
