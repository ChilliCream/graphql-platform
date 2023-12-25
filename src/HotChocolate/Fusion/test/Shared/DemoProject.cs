using System.Diagnostics;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Shared.Accounts;
using HotChocolate.Fusion.Shared.Appointments;
using HotChocolate.Fusion.Shared.Patients;
using HotChocolate.Fusion.Shared.Products;
using HotChocolate.Fusion.Shared.Reviews;
using HotChocolate.Fusion.Shared.Shipping;
using HotChocolate.Fusion.Shared.Books;
using HotChocolate.Fusion.Shared.Authors;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities.Introspection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Fusion.Shared;

internal enum DemoProjectType
{
    Accounts,
    Reviews,
    Reviews2,
    Products,
    Shipping,
    Appointment,
    Patient1,
    Books,
    Authors,
    Count,
}

internal readonly struct DemoProjectValues<T>
{
    public required T[] All { get; init; }

    public static DemoProjectValues<T> Create()
    {
        var all = new T[(int)DemoProjectType.Count];
        return new DemoProjectValues<T>
        {
            All = all,
        };
    }

    public ref T this[DemoProjectType type]
    {
        get => ref All[(int)type];
    }

    public bool AllSet => All.All(x => x != null);

    public ref T Accounts => ref All[(int)DemoProjectType.Accounts];
    public ref T Reviews => ref All[(int)DemoProjectType.Reviews];
    public ref T Reviews2 => ref All[(int)DemoProjectType.Reviews2];
    public ref T Products => ref All[(int)DemoProjectType.Products];
    public ref T Shipping => ref All[(int)DemoProjectType.Shipping];
    public ref T Appointment => ref All[(int)DemoProjectType.Appointment];
    public ref T Patient1 => ref All[(int)DemoProjectType.Patient1];
    public ref T Books => ref All[(int)DemoProjectType.Books];
    public ref T Authors => ref All[(int)DemoProjectType.Authors];
}

public sealed class DemoProject : IDisposable
{
    private readonly List<IDisposable> _disposables;
    private bool _disposed;
    private readonly DemoProjectValues<DemoSubgraph> _subgraphs;

    private DemoProject(
        List<IDisposable> disposables,
        DemoProjectValues<DemoSubgraph> subgraphs,
        IHttpClientFactory clientFactory,
        IWebSocketConnectionFactory webSocketConnectionFactory)
    {
        _subgraphs = subgraphs;
        _disposables = disposables;
        HttpClientFactory = clientFactory;
        WebSocketConnectionFactory = webSocketConnectionFactory;
    }

    public IHttpClientFactory HttpClientFactory { get; }
    public IWebSocketConnectionFactory WebSocketConnectionFactory { get; }

    public DemoSubgraph Reviews => _subgraphs.Reviews;
    public DemoSubgraph Reviews2 => _subgraphs.Reviews2;
    public DemoSubgraph Products => _subgraphs.Products;
    public DemoSubgraph Accounts => _subgraphs.Accounts;
    public DemoSubgraph Shipping => _subgraphs.Shipping;
    public DemoSubgraph Appointment => _subgraphs.Appointment;
    public DemoSubgraph Patient1 => _subgraphs.Patient1;
    public DemoSubgraph Books => _subgraphs.Books;
    public DemoSubgraph Authors => _subgraphs.Authors;

    public static async Task<DemoProject> CreateAsync(CancellationToken ct = default)
    {
        var disposables = new List<IDisposable>();
        TestServerFactory testServerFactory = new();
        disposables.Add(testServerFactory);

        var testServers = DemoProjectValues<TestServer>.Create();
        testServers.Reviews = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ReviewRepository>()
                .AddGraphQLServer()
                .AddQueryType<ReviewsQuery>()
                .AddMutationType<ReviewsMutation>()
                .AddSubscriptionType<ReviewsSubscription>()
                .AddMutationConventions()
                .AddGlobalObjectIdentification()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        testServers.Reviews2 = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<Reviews2.ReviewRepository>()
                .AddGraphQLServer()
                .AddQueryType<Reviews2.ReviewsQuery>()
                .AddMutationType<Reviews2.ReviewsMutation>()
                .AddSubscriptionType<Reviews2.ReviewsSubscription>()
                .AddMutationConventions()
                .AddGlobalObjectIdentification()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        testServers.Accounts = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<UserRepository>()
                .AddGraphQLServer()
                .AddQueryType<AccountQuery>()
                .AddMutationType<AccountMutation>()
                .AddMutationConventions()
                .AddGlobalObjectIdentification()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        testServers.Products = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ProductRepository>()
                .AddGraphQLServer()
                .AddQueryType<ProductQuery>()
                .AddMutationType<ProductMutation>()
                .AddGlobalObjectIdentification()
                .AddMutationConventions()
                .AddUploadType()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        testServers.Shipping = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<ShippingQuery>()
                .ConfigureSchema(b => b.SetContextData(GlobalIdSupportEnabled, 1))
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        testServers.Appointment = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<AppointmentQuery>()
                .AddObjectType<Appointments.Patient1>()
                .AddObjectType<Patient2>()
                .AddGlobalObjectIdentification()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        testServers.Patient1 = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<Patient1Query>()
                .AddGlobalObjectIdentification()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        testServers.Books = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<BookQuery>()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        testServers.Authors = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<AuthorQuery>()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        Debug.Assert(testServers.AllSet);

        var httpClients = new Dictionary<string, Func<HttpClient>>((int)DemoProjectType.Count);
        var webSocketClients = new Dictionary<string, Func<IWebSocketConnection>>((int)DemoProjectType.Count);
        var subgraphs = DemoProjectValues<DemoSubgraph>.Create();
        {
            var httpBaseAddress = new Uri("http://localhost:5000/graphql");
            var wsBaseAddress = new Uri("ws://localhost:5000/graphql");
            for (var i = (DemoProjectType)0; i < DemoProjectType.Count; i++)
            {
                var testServer = testServers[i];

                disposables.Add(testServer);

                var name = i.ToString();
                httpClients.Add(name, () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = testServer.CreateClient();
                    httpClient.BaseAddress = httpBaseAddress;
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                });
                webSocketClients.Add(name, () =>
                {
                    var wsClient = testServer.CreateWebSocketClient();
                    return new MockWebSocketConnection(wsClient);
                });

                var client = testServer.CreateClient();
                client.BaseAddress = httpBaseAddress;
                var schema = await IntrospectionClient
                    .IntrospectServerAsync(client, ct)
                    .ConfigureAwait(false);

                subgraphs[i] = new DemoSubgraph(
                    name,
                    client.BaseAddress,
                    wsBaseAddress,
                    schema,
                    testServer);
            }
        }

        Debug.Assert(subgraphs.AllSet);

        return new DemoProject(
            disposables,
            subgraphs,
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
