using System.Diagnostics.Contracts;
using HotChocolate.Fusion.Schemas.Accounts;
using HotChocolate.Fusion.Schemas.Reviews;
using HotChocolate.Utilities.Introspection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class DemoProject : IDisposable
{
    private readonly IReadOnlyList<IDisposable> _disposables;
    private readonly IHttpClientFactory _clientFactory;
    private bool _disposed;

    private DemoProject(
        IReadOnlyList<IDisposable> disposables,
        DemoSubgraph accounts,
        DemoSubgraph reviews,
        IHttpClientFactory clientFactory)
    {
        _disposables = disposables;
        Accounts = accounts;
        Reviews = reviews;
        _clientFactory = clientFactory;
    }

    public IHttpClientFactory HttpClientFactory => _clientFactory;

    public DemoSubgraph Reviews { get; }

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
        };

        return new DemoProject(
            disposables,
            new DemoSubgraph("Accounts", accountsClient.BaseAddress, accountsSchema, accounts),
            new DemoSubgraph("Reviews", reviewsClient.BaseAddress, reviewsSchema, reviews),
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
