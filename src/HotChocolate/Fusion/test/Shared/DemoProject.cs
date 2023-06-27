using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Fusion.Clients;
using HotChocolate.Fusion.Shared.Accounts;
using HotChocolate.Fusion.Shared.Appointments;
using HotChocolate.Fusion.Shared.Products;
using HotChocolate.Fusion.Shared.Reviews;
using HotChocolate.Fusion.Shared.Shipping;
using HotChocolate.Utilities.Introspection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Fusion.Shared;

public sealed class DemoProject : IDisposable
{
    private readonly IReadOnlyList<IDisposable> _disposables;
    private bool _disposed;

    private DemoProject(
        IReadOnlyList<IDisposable> disposables,
        DemoSubgraph accounts,
        DemoSubgraph reviews,
        DemoSubgraph reviews2,
        DemoSubgraph products,
        DemoSubgraph shipping,
        DemoSubgraph appointment,
        IHttpClientFactory clientFactory,
        IWebSocketConnectionFactory webSocketConnectionFactory)
    {
        _disposables = disposables;
        Accounts = accounts;
        Reviews = reviews;
        Reviews2 = reviews2;
        Products = products;
        Shipping = shipping;
        Appointment = appointment;
        HttpClientFactory = clientFactory;
        WebSocketConnectionFactory = webSocketConnectionFactory;
    }

    public IHttpClientFactory HttpClientFactory { get; }

    public IWebSocketConnectionFactory WebSocketConnectionFactory { get; }

    public DemoSubgraph Reviews { get; }

    public DemoSubgraph Reviews2 { get; }

    public DemoSubgraph Products { get; }

    public DemoSubgraph Accounts { get; }

    public DemoSubgraph Shipping { get; }

    public DemoSubgraph Appointment { get; }

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
                .AddMutationConventions()
                .AddGlobalObjectIdentification(),
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

        var reviews2 = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<Reviews2.ReviewRepository>()
                .AddGraphQLServer()
                .AddQueryType<Reviews2.ReviewsQuery>()
                .AddMutationType<Reviews2.ReviewsMutation>()
                .AddSubscriptionType<Reviews2.ReviewsSubscription>()
                .AddMutationConventions()
                .AddGlobalObjectIdentification(),
            c => c
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(reviews2);

        var reviews2Client = reviews2.CreateClient();
        reviews2Client.BaseAddress = new Uri("http://localhost:5000/graphql");
        var reviews2Schema = await introspection
            .DownloadSchemaAsync(reviews2Client, ct)
            .ConfigureAwait(false);

        var accounts = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<UserRepository>()
                .AddGraphQLServer()
                .AddQueryType<AccountQuery>()
                .AddMutationType<AccountMutation>()
                .AddMutationConventions()
                .AddGlobalObjectIdentification(),
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
                .AddQueryType<ProductQuery>()
                .AddGlobalObjectIdentification(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(products);

        var productsClient = products.CreateClient();
        productsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var productsSchema = await introspection
            .DownloadSchemaAsync(productsClient, ct)
            .ConfigureAwait(false);

        var shipping = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<ShippingQuery>()
                .ConfigureSchema(b => b.SetContextData(GlobalIdSupportEnabled, 1)),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(products);

        var shippingClient = shipping.CreateClient();
        shippingClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var shippingSchema = await introspection
            .DownloadSchemaAsync(shippingClient, ct)
            .ConfigureAwait(false);

        var appointment = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<AppointmentQuery>()
                .AddObjectType<PatientDE>()
                .AddObjectType<PatientCH>()
                .AddGlobalObjectIdentification(),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(products);

        var appointmentClient = appointment.CreateClient();
        appointmentClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var appointmentSchema = await introspection
            .DownloadSchemaAsync(appointmentClient, ct)
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
                "Reviews2", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = reviews2.CreateClient();
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
            {
                "Shipping", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = shipping.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    return httpClient;
                }
            },
            {
                "Appointment", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = appointment.CreateClient();
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
            {
                "Shipping", () => new MockWebSocketConnection(shipping.CreateWebSocketClient())
            },
            {
                "Appointment", () => new MockWebSocketConnection(appointment.CreateWebSocketClient())
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
                "Reviews2",
                reviews2Client.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                reviews2Schema,
                reviews2),
            new DemoSubgraph(
                "Products",
                productsClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                productsSchema,
                products),
            new DemoSubgraph(
                "Shipping",
                shippingClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                shippingSchema,
                shipping),
            new DemoSubgraph(
                "Appointment",
                appointmentClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                appointmentSchema,
                appointment),
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
