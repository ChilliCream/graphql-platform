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
using HotChocolate.Fusion.Shared.Resale;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Descriptors;
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
        DemoSubgraph shipping2,
        DemoSubgraph appointment,
        DemoSubgraph patient1,
        DemoSubgraph books,
        DemoSubgraph authors,
        DemoSubgraph resale,
        IHttpClientFactory clientFactory,
        IWebSocketConnectionFactory webSocketConnectionFactory)
    {
        _disposables = disposables;
        Accounts = accounts;
        Reviews = reviews;
        Reviews2 = reviews2;
        Products = products;
        Shipping = shipping;
        Shipping2 = shipping2;
        Appointment = appointment;
        Patient1 = patient1;
        Books = books;
        Authors = authors;
        Resale = resale;
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

    public DemoSubgraph Shipping2 { get; }

    public DemoSubgraph Appointment { get; }

    public DemoSubgraph Patient1 { get; }

    public DemoSubgraph Books { get; }

    public DemoSubgraph Authors { get; }

    public DemoSubgraph Resale { get; }

    public static async Task<DemoProject> CreateAsync(CancellationToken ct = default)
    {
        var disposables = new List<IDisposable>();
        TestServerFactory testServerFactory = new();
        disposables.Add(testServerFactory);

        var reviews = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ReviewRepository>()
                .AddGraphQLServer(disableDefaultSecurity: true)
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
        disposables.Add(reviews);

        var reviewsClient = reviews.CreateClient();
        reviewsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var reviewsSchema = await IntrospectionClient
            .IntrospectServerAsync(reviewsClient, ct)
            .ConfigureAwait(false);

        var reviews2 = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<Reviews2.ReviewRepository>()
                .AddGraphQLServer(disableDefaultSecurity: true)
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
        disposables.Add(reviews2);

        var reviews2Client = reviews2.CreateClient();
        reviews2Client.BaseAddress = new Uri("http://localhost:5000/graphql");
        var reviews2Schema = await IntrospectionClient
            .IntrospectServerAsync(reviews2Client, ct)
            .ConfigureAwait(false);

        var accounts = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<UserRepository>()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<AccountQuery>()
                .AddMutationType<AccountMutation>()
                .AddMutationConventions()
                .AddGlobalObjectIdentification()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(accounts);

        var accountsClient = accounts.CreateClient();
        accountsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var accountsSchema = await IntrospectionClient
            .IntrospectServerAsync(accountsClient, ct)
            .ConfigureAwait(false);

        var products = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddSingleton<ProductRepository>()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<ProductQuery>()
                .AddMutationType<ProductMutation>()
                .AddGlobalObjectIdentification()
                .AddMutationConventions()
                .AddUploadType()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(products);

        var productsClient = products.CreateClient();
        productsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var productsSchema = await IntrospectionClient
            .IntrospectServerAsync(productsClient, ct)
            .ConfigureAwait(false);

        var shipping = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<ShippingQuery>()
                .ConfigureSchema(b => b.SetContextData(GlobalIdSupportEnabled, 1))
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(shipping);

        var shippingClient = shipping.CreateClient();
        shippingClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var shippingSchema = await IntrospectionClient
            .IntrospectServerAsync(shippingClient, ct)
            .ConfigureAwait(false);

        var shipping2 = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<Shipping2.ShippingQuery>()
                .ConfigureSchema(b => b.SetContextData(GlobalIdSupportEnabled, 1))
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(shipping2);

        var shipping2Client = shipping2.CreateClient();
        shipping2Client.BaseAddress = new Uri("http://localhost:5000/graphql");
        var response = await shipping2Client.GetAsync("http://localhost:5000/graphql?sdl", ct);
        var buffer = await response.Content.ReadAsByteArrayAsync(ct);
        var shipping2Schema = Utf8GraphQLParser.Parse(buffer);

        var appointment = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<AppointmentQuery>()
                .AddObjectType<Appointments.Patient1>()
                .AddObjectType<Patient2>()
                .AddGlobalObjectIdentification()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(appointment);

        var appointmentClient = appointment.CreateClient();
        appointmentClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var appointmentSchema = await IntrospectionClient
            .IntrospectServerAsync(appointmentClient, ct)
            .ConfigureAwait(false);

        var patient1 = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<Patient1Query>()
                .AddGlobalObjectIdentification()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(patient1);

        var patient1Client = patient1.CreateClient();
        patient1Client.BaseAddress = new Uri("http://localhost:5000/graphql");
        var patient1Schema = await IntrospectionClient
            .IntrospectServerAsync(patient1Client, ct)
            .ConfigureAwait(false);

        var books = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<BookQuery>()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(books);

        var booksClient = books.CreateClient();
        booksClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var booksSchema = await IntrospectionClient
            .IntrospectServerAsync(booksClient, ct)
            .ConfigureAwait(false);

        var authors = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<AuthorQuery>()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(authors);

        var authorsClient = authors.CreateClient();
        authorsClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var authorsSchema = await IntrospectionClient
            .IntrospectServerAsync(authorsClient, ct)
            .ConfigureAwait(false);

        var resale = testServerFactory.Create(
            s => s
                .AddRouting()
                .AddGraphQLServer(disableDefaultSecurity: true)
                .AddQueryType<ResaleQuery>()
                .AddGlobalObjectIdentification()
                .AddMutationConventions()
                .AddUploadType()
                .AddConvention<INamingConventions>(_ => new DefaultNamingConventions()),
            c => c
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
        disposables.Add(resale);

        var resaleClient = resale.CreateClient();
        resaleClient.BaseAddress = new Uri("http://localhost:5000/graphql");
        var resaleSchema = await IntrospectionClient
            .IntrospectServerAsync(resaleClient, ct)
            .ConfigureAwait(false);

        var httpClients = new Dictionary<string, Func<HttpClient>>
        {
            {
                "Reviews", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = reviews.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Reviews2", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = reviews2.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Accounts", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = accounts.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Products", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = products.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Shipping", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = shipping.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Shipping2", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = shipping2.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Appointment", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = appointment.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Patient1", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = patient1.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Books", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = books.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Authors", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = books.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
            {
                "Resale", () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var httpClient = resale.CreateClient();
                    httpClient.BaseAddress = new Uri("http://localhost:5000/graphql");
                    httpClient.DefaultRequestHeaders.AddGraphQLPreflight();
                    return httpClient;
                }
            },
        };

        var webSocketClients = new Dictionary<string, Func<IWebSocketConnection>>
        {
            { "Reviews", () => new MockWebSocketConnection(reviews.CreateWebSocketClient()) },
            { "Reviews2", () => new MockWebSocketConnection(reviews2.CreateWebSocketClient()) },
            { "Accounts", () => new MockWebSocketConnection(accounts.CreateWebSocketClient()) },
            { "Products", () => new MockWebSocketConnection(products.CreateWebSocketClient()) },
            { "Shipping", () => new MockWebSocketConnection(shipping.CreateWebSocketClient()) },
            { "Appointment", () => new MockWebSocketConnection(appointment.CreateWebSocketClient()) },
            { "Patient1", () => new MockWebSocketConnection(patient1.CreateWebSocketClient()) },
            { "Books", () => new MockWebSocketConnection(books.CreateWebSocketClient()) },
            { "Authors", () => new MockWebSocketConnection(authors.CreateWebSocketClient()) },
            { "Resale", () => new MockWebSocketConnection(resale.CreateWebSocketClient()) },
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
                "Shipping2",
                shipping2Client.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                shipping2Schema,
                shipping2),
            new DemoSubgraph(
                "Appointment",
                appointmentClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                appointmentSchema,
                appointment),
            new DemoSubgraph(
                "Patient1",
                patient1Client.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                patient1Schema,
                patient1),
            new DemoSubgraph(
                "Books",
                booksClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                booksSchema,
                books),
            new DemoSubgraph(
                "Authors",
                authorsClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                authorsSchema,
                authors),
            new DemoSubgraph(
                "Resale",
                resaleClient.BaseAddress,
                new Uri("ws://localhost:5000/graphql"),
                resaleSchema,
                resale),
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
