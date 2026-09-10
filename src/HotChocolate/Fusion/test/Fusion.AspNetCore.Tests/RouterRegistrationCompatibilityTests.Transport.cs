using System.Net;
using System.Net.Http.Json;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Formatters;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Transport.Formatters;
using HotChocolate.Transport.Sockets;
using HotChocolate.Transport.Sockets.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OperationResult = HotChocolate.Execution.OperationResult;
using TransportOperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

public partial class RouterRegistrationCompatibilityTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    public async Task AddInterceptors_Should_UseSchemaServicesAndPreserveIdentity_When_UsingEitherBuilderSurface(
        int surface,
        bool factory)
    {
        using var session = new TestServerSession();
        using var server = CreateServer(session, router =>
        {
            if (surface == 0)
            {
                Assert.Same(router, factory
                    ? router.AddHttpRequestInterceptor(sp => new MarkingHttpInterceptor(sp.GetRequiredService<Marker>().Value))
                    : router.AddHttpRequestInterceptor<MarkingHttpInterceptor>());
                Assert.Same(router, factory
                    ? router.AddSocketSessionInterceptor(sp => new MarkingSocketInterceptor(sp.GetRequiredService<Marker>()))
                    : router.AddSocketSessionInterceptor<MarkingSocketInterceptor>());
            }
            else
            {
#pragma warning disable CS0618 // Both static legacy calls and legacy-only receivers must stay functional.
                IFusionGatewayBuilder legacy = surface == 1
                    ? router
                    : new LegacyOnlyBuilder(router.Name, router.Services);
                Assert.Same(legacy, factory
                    ? AspNetCoreFusionGatewayBuilderExtensions.AddHttpRequestInterceptor(
                        legacy, sp => new MarkingHttpInterceptor(sp.GetRequiredService<Marker>().Value))
                    : legacy.AddHttpRequestInterceptor<MarkingHttpInterceptor>());
                Assert.Same(legacy, factory
                    ? AspNetCoreFusionGatewayBuilderExtensions.AddSocketSessionInterceptor(
                        legacy, sp => new MarkingSocketInterceptor(sp.GetRequiredService<Marker>()))
                    : legacy.AddSocketSessionInterceptor<MarkingSocketInterceptor>());
#pragma warning restore CS0618
            }

            router.UseRequest((_, next) => context =>
            {
                if (context.Request.Extensions is { } extensions)
                {
                    context.Result = new OperationResult(
                        ImmutableOrderedDictionary<string, object?>.Empty.Add(
                            "captured", extensions.Document.RootElement.GetProperty("captured").GetString()));
                    return default;
                }

                return next(context);
            }, key: "CaptureSocketInterceptor");
        });

        using var http = server.CreateClient();
        using var response = await http.PostAsJsonAsync(
            "/graphql", new { query = "{ __typename }" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(factory ? "named" : "generic", Assert.Single(response.Headers.GetValues("interceptor")));
        Assert.Equal("{\"data\":{\"__typename\":\"Query\"}}", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var socketClient = server.CreateWebSocketClient();
        socketClient.ConfigureRequest = r => r.Headers.SecWebSocketProtocol = WellKnownProtocols.GraphQL_Transport_WS;
        using var socket = await socketClient.ConnectAsync(new Uri("ws://localhost/graphql"), TestContext.Current.CancellationToken);
        await using var client = await SocketClient.ConnectAsync(socket, TestContext.Current.CancellationToken);
        using var results = await client.ExecuteAsync(new TransportOperationRequest("{ __typename }"), TestContext.Current.CancellationToken);
        var received = 0;
        await foreach (var result in results.ReadResultsAsync().WithCancellation(TestContext.Current.CancellationToken))
        {
            using (result)
            {
                result.MatchInlineSnapshot(
                    """
                    {
                      "extensions": {
                        "captured": "named"
                      }
                    }
                    """);
                received++;
            }
        }

        Assert.Equal(1, received);
    }

    public static IEnumerable<object[]> FormatterShapes()
    {
        for (var surface = 0; surface < 3; surface++)
        {
            for (var overload = 0; overload < 4; overload++)
            {
                yield return [surface, overload];
            }
        }
    }

    [Theory]
    [MemberData(nameof(FormatterShapes))]
    public async Task AddHttpResponseFormatter_Should_FormatActualResponses_When_UsingAnyOverloadOrBuilderSurface(
        int surface,
        int overload)
    {
        using var session = new TestServerSession();
        using var server = CreateServer(session, router =>
        {
            var options = new HttpResponseFormatterOptions
            {
                Json = new JsonResultFormatterOptions { Indented = true }
            };
            if (surface == 0)
            {
                IFusionRouterBuilder result = overload switch
                {
                    0 => router.AddHttpResponseFormatter(indented: true, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1),
                    1 => AspNetCoreFusionRouterBuilderExtensions.AddHttpResponseFormatter(router, options: options, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1),
                    2 => router.AddHttpResponseFormatter<TeapotFormatter>(),
                    3 => router.AddHttpResponseFormatter(factory: sp => new TeapotFormatter(sp.GetRequiredService<Marker>())),
                    _ => throw new ArgumentOutOfRangeException(nameof(overload))
                };
                Assert.Same(router, result);
            }
            else
            {
#pragma warning disable CS0618 // Verify all retained formatter overloads with a genuine legacy-only builder.
                IFusionGatewayBuilder legacy = surface == 1
                    ? router
                    : new LegacyOnlyBuilder(router.Name, router.Services);
                var result = overload switch
                {
                    0 => AspNetCoreFusionGatewayBuilderExtensions.AddHttpResponseFormatter(legacy, indented: true, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1),
                    1 => legacy.AddHttpResponseFormatter(options: options, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1),
                    2 => AspNetCoreFusionGatewayBuilderExtensions.AddHttpResponseFormatter<TeapotFormatter>(legacy),
                    3 => legacy.AddHttpResponseFormatter(factory: sp => new TeapotFormatter(sp.GetRequiredService<Marker>())),
                    _ => throw new ArgumentOutOfRangeException(nameof(overload))
                };
                Assert.Same(legacy, result);
#pragma warning restore CS0618
            }
        });

        using var client = server.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/graphql", new { query = "{ __typename }" }, TestContext.Current.CancellationToken);
        Assert.Equal(overload < 2 ? HttpStatusCode.OK : (HttpStatusCode)418, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        if (overload < 2)
        {
            body.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "__typename": "Query"
                  }
                }
                """);

            using var source = CreateSourceSchema("A", "type Query { item: Item } type Item { name: String field: String }");
            using var gateway = await CreateCompositeSchemaAsync(
                [("A", source)],
                configureGatewayBuilder: router =>
                {
                    router.AddHttpRequestInterceptor<DefaultHttpRequestInterceptor>();
                    var options = new HttpResponseFormatterOptions
                    {
                        Json = new JsonResultFormatterOptions { Indented = true }
                    };
                    if (surface == 0)
                    {
                        Assert.Same(router, overload == 0
                            ? router.AddHttpResponseFormatter(indented: true, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1)
                            : router.AddHttpResponseFormatter(options: options, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1));
                    }
                    else
                    {
#pragma warning disable CS0618 // Observe the retained incremental-delivery default on legacy-only builders too.
                        IFusionGatewayBuilder legacy = surface == 1
                            ? router
                            : new LegacyOnlyBuilder(router.Name, router.Services);
                        Assert.Same(legacy, overload == 0
                            ? legacy.AddHttpResponseFormatter(indented: true, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1)
                            : legacy.AddHttpResponseFormatter(options: options, incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1));
#pragma warning restore CS0618
                    }
                });
            using var incrementalClient = gateway.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
            {
                Content = JsonContent.Create(new
                {
                    query = "{ item { name ... @defer(label: \"later\") { field } } }"
                })
            };
            request.Headers.Add("Accept", "multipart/mixed");
            using var incremental = await incrementalClient.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, incremental.StatusCode);
            var incrementalBody = await incremental.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            incrementalBody.Replace("\r\n", "\n", StringComparison.Ordinal).MatchMarkdownSnapshot("Incremental");
        }
        else
        {
            Assert.Equal("{\"data\":{\"__typename\":\"Query\"}}", body);
        }
    }

    [Fact]
    public void Extensions_Should_PreserveArgumentExceptions_When_UsingLegacyOnlyAndRouterBuilders()
    {
        var router = new ServiceCollection().AddGraphQLRouter();
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() =>
            AspNetCoreFusionRouterBuilderExtensions.AddHttpRequestInterceptor<MarkingHttpInterceptor>(null!)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() => router.AddHttpRequestInterceptor(null!)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() => router.AddSocketSessionInterceptor<MarkingSocketInterceptor>(null!)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() => router.AddHttpResponseFormatter<TeapotFormatter>(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => router.ModifyServerOptions(null!)).ParamName);

#pragma warning disable CS0618 // Null validation must also work without a router implementation.
        var legacy = new LegacyOnlyBuilder(router.Name, router.Services);
        Assert.Equal("builder", Assert.Throws<ArgumentNullException>(() =>
            AspNetCoreFusionGatewayBuilderExtensions.AddHttpRequestInterceptor<MarkingHttpInterceptor>(null!)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() => legacy.AddHttpRequestInterceptor(null!)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() => legacy.AddSocketSessionInterceptor<MarkingSocketInterceptor>(null!)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() => legacy.AddHttpResponseFormatter<TeapotFormatter>(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => legacy.ModifyServerOptions(null!)).ParamName);
#pragma warning restore CS0618
    }

    private static TestServer CreateServer(TestServerSession session, Action<IFusionRouterBuilder> configure)
        => session.CreateServer(
            services =>
            {
                services.AddRouting();
                services.AddHttpClient();
                services.AddSingleton(new Marker("application"));
                var router = services.AddGraphQLRouter("named")
                    .AddInMemoryConfiguration(s_schema)
                    .ConfigureSchemaServices((_, sc) => sc.AddSingleton(new Marker("named")));
                configure(router);
            },
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQL(schemaName: "named"));
            });

    private sealed record Marker(string Value);

    private sealed class MarkingHttpInterceptor : DefaultHttpRequestInterceptor
    {
        private readonly string _value;

        public MarkingHttpInterceptor() : this("generic")
        {
        }

        public MarkingHttpInterceptor(string value)
        {
            _value = value;
        }

        public override async ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
            context.Response.Headers["interceptor"] = _value;
        }
    }

    private sealed class MarkingSocketInterceptor(Marker marker) : DefaultSocketSessionInterceptor
    {
        public override ValueTask OnRequestAsync(
            ISocketSession session,
            string operationSessionId,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.SetExtensions(new Dictionary<string, object?> { ["captured"] = marker.Value });
            return default;
        }
    }

    private sealed class TeapotFormatter(Marker marker) : DefaultHttpResponseFormatter
    {
        protected override HttpStatusCode OnDetermineStatusCode(
            OperationResult result,
            FormatInfo format,
            HttpStatusCode? proposedStatusCode)
            => marker.Value == "named" ? (HttpStatusCode)418 : HttpStatusCode.InternalServerError;
    }
}
