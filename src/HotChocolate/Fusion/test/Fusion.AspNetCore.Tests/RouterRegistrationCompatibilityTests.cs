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
        // arrange
        var host = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            EnvironmentName = Environments.Production
        });
        var name = nonDefaults ? "named" : ISchemaDefinition.DefaultName;
        host.Services.AddHttpClient();

        // act
#pragma warning disable CS0618 // Exercise the exact legacy registration returns alongside the new entry points.
        var builder = Register(host, shape, nonDefaults);
        Assert.Same(host.Services, builder.Services);
        builder.AddInMemoryConfiguration(s_schema);
#pragma warning restore CS0618

        host.Services.AddGraphQLRouter("other").AddInMemoryConfiguration(s_schema);
        await using var services = host.Services.BuildServiceProvider();
        var provider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await provider.GetExecutorAsync(name, TestContext.Current.CancellationToken);
        var other = await provider.GetExecutorAsync("other", TestContext.Current.CancellationToken);

        await using var result = await executor.ExecuteAsync(
            "{ __schema { queryType { name } } }", TestContext.Current.CancellationToken);
        await using var otherResult = await other.ExecuteAsync(
            "{ __schema { queryType { name } } }", TestContext.Current.CancellationToken);

        // The same valid body is accepted with defaults and rejected with the explicit 512-byte limit.
        var body = Encoding.UTF8.GetBytes("{\"query\":\"{ __typename }\"}" + new string(' ', 1024));
        var parser = executor.Schema.Services.GetRequiredService<IHttpRequestParser>();
        var otherParser = other.Schema.Services.GetRequiredService<IHttpRequestParser>();

        // assert
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

        AssertIntrospectionDisabled(otherResult);

        var summary = new
        {
            BuilderName = builder.Name,
            ExecutorSchemaName = executor.Schema.Name,
            OtherSchemaName = other.Schema.Name,
            LargeBodyParseResult = await DescribeParseResultAsync(parser, body),
            OtherLargeBodyParseResult = await DescribeParseResultAsync(otherParser, body)
        };

        if (nonDefaults)
        {
            summary.MatchInlineSnapshot(
                """
                {
                  "BuilderName": "named",
                  "ExecutorSchemaName": "named",
                  "OtherSchemaName": "other",
                  "LargeBodyParseResult": "Rejected: Request size exceeds maximum allowed size.",
                  "OtherLargeBodyParseResult": "{\n  __typename\n}"
                }
                """);
        }
        else
        {
            summary.MatchInlineSnapshot(
                """
                {
                  "BuilderName": "_Default",
                  "ExecutorSchemaName": "_Default",
                  "OtherSchemaName": "other",
                  "LargeBodyParseResult": "{\n  __typename\n}",
                  "OtherLargeBodyParseResult": "{\n  __typename\n}"
                }
                """);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Configure_Should_KeepIdentityOrderAndOneNamedPipeline_When_LegacyAndRouterBuildersMix(
        bool customLegacyBuilder)
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient();
        var router = services.AddGraphQLRouter("one").AddInMemoryConfiguration(s_schema);

        // act
        Assert.Same(router, router.ModifyServerOptions(o => o.MaxConcurrentExecutions = 3));
        Assert.Same(router, router.ModifyOptions(o => o.OperationDocumentCacheSize = 64));

#pragma warning disable CS0618 // Verify legacy-only builders and third-party base-interface continuation.
        IFusionGatewayBuilder legacy = customLegacyBuilder
            ? new LegacyOnlyBuilder(router.Name, services)
            : router;
        (legacy is IFusionRouterBuilder).MatchInlineSnapshot(customLegacyBuilder ? "false" : "true");
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

        // assert
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
        // arrange
        var services = new ServiceCollection();

        // act
        string[] entries =
        [
            Capture(
                "FusionServerServiceCollectionExtensions.AddGraphQLRouter(services: null)",
                () => FusionServerServiceCollectionExtensions.AddGraphQLRouter(null!)),
            Capture(
                "services.AddGraphQLRouter(maxAllowedRequestSize: -1)",
                () => services.AddGraphQLRouter(maxAllowedRequestSize: -1)),
            Capture(
                "FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLRouter(host: null)",
                () => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLRouter(null!)),
#pragma warning disable CS0618 // The legacy registration must retain its original exception semantics.
            Capture(
                "FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(services: null)",
                () => FusionServerServiceCollectionExtensions.AddGraphQLGatewayServer(null!)),
            Capture(
                "services.AddGraphQLGatewayServer(maxAllowedRequestSize: -1)",
                () => services.AddGraphQLGatewayServer(maxAllowedRequestSize: -1)),
            Capture(
                "FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(host: null)",
                () => FusionServerAspNetCoreHostingBuilderExtensions.AddGraphQLGateway(null!))
#pragma warning restore CS0618
        ];

        // assert
        entries.MatchMarkdownSnapshot();
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

    private static async Task<string> DescribeParseResultAsync(IHttpRequestParser parser, byte[] body)
    {
        try
        {
            var requests = await ParseAsync(parser, body);
            return requests.Length == 1
                ? requests[0].Document?.ToString() ?? "<null-document>"
                : $"<{requests.Length}-requests>";
        }
        catch (GraphQLRequestException ex)
        {
            return $"Rejected: {ex.Message}";
        }
    }

    /// <summary>
    /// Invokes <paramref name="action"/> and projects the outcome as
    /// "&lt;entryPoint&gt; -&gt; &lt;ExceptionType&gt;(&lt;ParamName&gt;)" for snapshotting,
    /// so exception-shape assertions read as data rather than a chain of Assert calls.
    /// </summary>
    private static string Capture(string entryPoint, Action action)
    {
        try
        {
            action();
            return $"{entryPoint} -> <no exception>";
        }
        catch (Exception ex)
        {
            var paramName = (ex as ArgumentException)?.ParamName;
            return paramName is null
                ? $"{entryPoint} -> {ex.GetType().Name}"
                : $"{entryPoint} -> {ex.GetType().Name}({paramName})";
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
