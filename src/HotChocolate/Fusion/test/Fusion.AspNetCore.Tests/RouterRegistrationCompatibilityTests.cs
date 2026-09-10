using System.IO.Pipelines;
using System.Text;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Parsers;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion;

public partial class RouterRegistrationCompatibilityTests : FusionTestBase
{
    private static readonly DocumentNode s_schema = Utf8GraphQLParser.Parse(
        """
        type Query @fusion__type(schema: A) {
          field: String @fusion__field(schema: A)
        }
        enum fusion__Schema { A }
        """);

    public static IEnumerable<object[]> RegistrationShapes()
    {
        for (var shape = 0; shape < 8; shape++)
        {
            yield return [shape, false];
            yield return [shape, true];
        }
    }

    [Theory]
    [MemberData(nameof(RegistrationShapes))]
    public async Task Register_Should_PreserveSecurityRequestSizeAndSchemaName_When_UsingAnyEntryPoint(
        int shape,
        bool nonDefaults)
    {
        var host = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            EnvironmentName = Environments.Production
        });
        var name = nonDefaults ? "named" : ISchemaDefinition.DefaultName;
        host.Services.AddHttpClient();

#pragma warning disable CS0618 // Exercise the exact legacy registration returns alongside the new entry points.
        var builder = Register(host, shape, nonDefaults);
        Assert.Equal(name, builder.Name);
        Assert.Same(host.Services, builder.Services);
        builder.AddInMemoryConfiguration(s_schema);
#pragma warning restore CS0618

        host.Services.AddGraphQLRouter("other").AddInMemoryConfiguration(s_schema);
        await using var services = host.Services.BuildServiceProvider();
        var provider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await provider.GetExecutorAsync(name, TestContext.Current.CancellationToken);
        var other = await provider.GetExecutorAsync("other", TestContext.Current.CancellationToken);
        Assert.Equal(name, executor.Schema.Name);
        Assert.Equal("other", other.Schema.Name);

        await using var result = await executor.ExecuteAsync(
            "{ __schema { queryType { name } } }", TestContext.Current.CancellationToken);
        if (nonDefaults)
        {
            result.MatchInlineSnapshot(
                """
                {
                  "data": {
                    "__schema": {
                      "queryType": {
                        "name": "Query"
                      }
                    }
                  }
                }
                """);
        }
        else
        {
            AssertIntrospectionDisabled(result);
        }

        await using var otherResult = await other.ExecuteAsync(
            "{ __schema { queryType { name } } }", TestContext.Current.CancellationToken);
        AssertIntrospectionDisabled(otherResult);

        // The same valid body is accepted with defaults and rejected with the explicit 512-byte limit.
        var body = Encoding.UTF8.GetBytes("{\"query\":\"{ __typename }\"}" + new string(' ', 1024));
        var parser = executor.Schema.Services.GetRequiredService<IHttpRequestParser>();
        var otherParser = other.Schema.Services.GetRequiredService<IHttpRequestParser>();
        if (nonDefaults)
        {
            var error = await Assert.ThrowsAsync<GraphQLRequestException>(() => ParseAsync(parser, body));
            Assert.Equal("Request size exceeds maximum allowed size.", error.Message);
        }
        else
        {
            Assert.Equal("{\n  __typename\n}", Assert.Single(await ParseAsync(parser, body)).Document?.ToString());
        }

        Assert.Equal("{\n  __typename\n}", Assert.Single(await ParseAsync(otherParser, body)).Document?.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Configure_Should_KeepIdentityOrderAndOneNamedPipeline_When_LegacyAndRouterBuildersMix(
        bool customLegacyBuilder)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var router = services.AddGraphQLRouter("one").AddInMemoryConfiguration(s_schema);
        Assert.Same(router, router.ModifyServerOptions(o => o.MaxConcurrentExecutions = 3));
        Assert.Same(router, router.ModifyOptions(o => o.OperationDocumentCacheSize = 64));

#pragma warning disable CS0618 // Verify legacy-only builders and third-party base-interface continuation.
        IFusionGatewayBuilder legacy = customLegacyBuilder
            ? new LegacyOnlyBuilder(router.Name, services)
            : router;
        Assert.Equal(!customLegacyBuilder, legacy is IFusionRouterBuilder);
        Assert.Same(legacy, AspNetCoreFusionGatewayBuilderExtensions.ModifyServerOptions(
            legacy, o => o.MaxConcurrentExecutions *= 2));
        Assert.Same(legacy, legacy.ModifyOptions(o => o.OperationDocumentCacheSize *= 2));
        Assert.Same(router, ReturnLegacyBuilder(router).ModifyServerOptions(o => o.MaxConcurrentExecutions += 1));
        services.AddGraphQLGatewayServer("one").ModifyOptions(o => o.OperationDocumentCacheSize += 16);
#pragma warning restore CS0618

        services.AddGraphQLRouter("two").AddInMemoryConfiguration(s_schema)
            .ModifyServerOptions(o => o.MaxConcurrentExecutions = 2)
            .ModifyOptions(o => o.OperationDocumentCacheSize = 32);

        await using var provider = services.BuildServiceProvider();
        var executors = provider.GetRequiredService<IRequestExecutorProvider>();
        var one = await executors.GetExecutorAsync("one", TestContext.Current.CancellationToken);
        var two = await executors.GetExecutorAsync("two", TestContext.Current.CancellationToken);
        var options = provider.GetRequiredService<IOptionsMonitor<GraphQLServerOptions>>();
        var setup = provider.GetRequiredService<IOptionsMonitor<FusionRouterSetup>>();

        new
        {
            executors.SchemaNames,
            ManagerCount = services.Count(s => s.ServiceType == typeof(FusionRequestExecutorManager)),
            OneConcurrency = options.Get("one").MaxConcurrentExecutions,
            TwoConcurrency = options.Get("two").MaxConcurrentExecutions,
            OneCacheSize = one.Schema.Features.GetRequired<FusionOptions>().OperationDocumentCacheSize,
            TwoCacheSize = two.Schema.Features.GetRequired<FusionOptions>().OperationDocumentCacheSize,
            OneOptionCallbacks = setup.Get("one").OptionsModifiers.Count,
            TwoOptionCallbacks = setup.Get("two").OptionsModifiers.Count
        }.MatchMarkdownSnapshot();

        await using var result = await one.ExecuteAsync("{ __typename }", TestContext.Current.CancellationToken);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "__typename": "Query"
              }
            }
            """);
    }

    [Fact]
    public void Register_Should_PreserveArgumentExceptions_When_UsingLegacyAndRouterEntryPoints()
    {
        var services = new ServiceCollection();
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
            FusionServerServiceCollectionExtensions.AddGraphQLRouter(null!)).ParamName);
        Assert.Equal("maxAllowedRequestSize", Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddGraphQLRouter(maxAllowedRequestSize: -1)).ParamName);
        Assert.Throws<NullReferenceException>(() =>
            FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLRouter(null!));

#pragma warning disable CS0618 // The legacy registration must retain its original exception semantics.
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
            FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(null!)).ParamName);
        Assert.Equal("maxAllowedRequestSize", Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddGraphQLGatewayServer(maxAllowedRequestSize: -1)).ParamName);
        Assert.Throws<NullReferenceException>(() =>
            FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(null!));
#pragma warning restore CS0618
    }

    private static void AssertIntrospectionDisabled(IExecutionResult result)
        => result.MatchInlineSnapshot(
            """
            {
              "errors": [
                {
                  "message": "Introspection is not allowed for the current request.",
                  "locations": [
                    {
                      "line": 1,
                      "column": 3
                    }
                  ],
                  "extensions": {
                    "code": "HC0046",
                    "field": "__schema"
                  }
                }
              ]
            }
            """);

    private static async Task<GraphQLRequest[]> ParseAsync(IHttpRequestParser parser, byte[] body)
    {
        await using var stream = new MemoryStream(body);
        var reader = PipeReader.Create(stream);
        try
        {
            return await parser.ParseRequestAsync(reader, false, TestContext.Current.CancellationToken);
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }

#pragma warning disable CS0618 // These helpers model the complete published legacy registration surface.
    private static IFusionGatewayBuilder Register(IHostApplicationBuilder host, int shape, bool nonDefaults)
        => (shape, nonDefaults) switch
        {
            (0, false) => host.Services.AddGraphQLRouter(),
            (0, true) => host.Services.AddGraphQLRouter(name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
            (1, false) => FusionServerServiceCollectionExtensions.AddGraphQLRouter(host.Services),
            (1, true) => FusionServerServiceCollectionExtensions.AddGraphQLRouter(host.Services, name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
            (2, false) => host.AddGraphQLRouter(),
            (2, true) => host.AddGraphQLRouter(name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
            (3, false) => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLRouter(host),
            (3, true) => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLRouter(host, name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
            (4, false) => host.Services.AddGraphQLGatewayServer(),
            (4, true) => host.Services.AddGraphQLGatewayServer(name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
            (5, false) => FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(host.Services),
            (5, true) => FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(host.Services, name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
            (6, false) => host.AddGraphQLGateway(),
            (6, true) => host.AddGraphQLGateway(name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
            (7, false) => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(host),
            (7, true) => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(host, name: "named", maxAllowedRequestSize: 512, disableDefaultSecurity: true),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };

    private static IFusionGatewayBuilder ReturnLegacyBuilder(IFusionGatewayBuilder builder) => builder;

    private sealed class LegacyOnlyBuilder(string name, IServiceCollection services) : IFusionGatewayBuilder
    {
        public string Name { get; } = name;

        public IServiceCollection Services { get; } = services;
    }
#pragma warning restore CS0618
}
