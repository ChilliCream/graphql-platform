using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

public sealed class GraphQLJavaScriptAppResourceBuilderExtensionsTests : IDisposable
{
    private readonly DirectoryInfo _appDirectory =
        Directory.CreateTempSubdirectory("fusion-js-app-tests-");

    [Fact]
    public void WithGraphQLHttpEndpoint_Should_RejectPath_When_PathIsNotRooted()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var app = builder.AddJavaScriptApp("shop", _appDirectory.FullName);

        // act
        var exception = Assert.Throws<ArgumentException>(
            () => app.WithGraphQLHttpEndpoint(path: "graphql"));

        // assert
        Assert.Equal(
            "The GraphQL endpoint path must start with '/'. (Parameter 'path')",
            exception.Message);
    }

    [Fact]
    public void WithGraphQLHttpEndpoint_Should_RejectSchemaPath_When_SchemaPathIsNotRooted()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var app = builder.AddJavaScriptApp("shop", _appDirectory.FullName);

        // act
        var exception = Assert.Throws<ArgumentException>(
            () => app.WithGraphQLHttpEndpoint(schemaPath: "schema.graphql"));

        // assert
        Assert.Equal(
            "The GraphQL schema endpoint path must start with '/'. (Parameter 'schemaPath')",
            exception.Message);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_PinEndpointPort_When_SettingsDeclareLoopbackTransportUrl()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop",
              "transports": {
                "http": {
                  "url": "{{SHOP_URL}}/api/graphql"
                }
              },
              "environments": {
                "Aspire": {
                  "SHOP_URL": "http://localhost:5734"
                }
              }
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal("http", endpoint.Name);
        Assert.Equal(5734, endpoint.Port);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_AnnotateAppForComposition_When_GatewayComposesApp()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop",
              "transports": {
                "http": {
                  "url": "https://shop.example.com/graphql",
                  "devUrl": "{{SHOP_URL}}/shop/graphql"
                }
              },
              "environments": {
                "Aspire": {
                  "SHOP_URL": "http://localhost:6123"
                }
              }
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var annotation = Assert.Single(
            app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        var directory = Assert.Single(
            app.Resource.Annotations.OfType<GraphQLSourceSchemaDirectoryAnnotation>());
        Assert.Equal(_appDirectory.FullName, directory.Directory);
        $"""
         GraphQLPath: {annotation.GraphQLPath}
         SchemaPath: {annotation.SchemaPath}
         EndpointName: {annotation.EndpointName}
         Location: {annotation.Location}
         EndpointPort: {app.Resource.Annotations.OfType<EndpointAnnotation>().Single().Port}
         """.MatchInlineSnapshot(
            """
            GraphQLPath: /graphql
            SchemaPath: /graphql/schema.graphql
            EndpointName: http
            Location: SchemaEndpoint
            EndpointPort: 6123
            """);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_PreserveConfiguration_When_ArgumentsAreProvided()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop"
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithHttpEndpoint(port: 4321)
            .WithGraphQLHttpEndpoint(
                path: "/api/graphql",
                schemaPath: "/api/schema.graphql",
                sourceSchemaName: "Shop");
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        // the endpoint the app declares itself stays untouched
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(4321, endpoint.Port);
        var annotation = Assert.Single(
            app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        $"""
         GraphQLPath: {annotation.GraphQLPath}
         SchemaPath: {annotation.SchemaPath}
         SourceSchemaName: {annotation.SourceSchemaName}
         """.MatchInlineSnapshot(
            """
            GraphQLPath: /api/graphql
            SchemaPath: /api/schema.graphql
            SourceSchemaName: Shop
            """);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_KeepSchemaPathNull_When_SchemaPathIsNull()
    {
        // arrange
        // a null schema path must survive as null, which an Apollo Federation source schema needs
        // because it serves its schema through the GraphQL endpoint.
        WriteSchemaSettings(
            """
            {
              "name": "Shop"
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint(path: "/api/graphql", schemaPath: null);
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var annotation = Assert.Single(
            app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Null(annotation.SchemaPath);
        Assert.Equal("/api/graphql", annotation.GraphQLPath);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_NotPinEndpointPort_When_SettingsDeclareNoTransportUrl()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop"
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Null(endpoint.Port);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_NotPinEndpointPort_When_SettingsUrlOmitsPort()
    {
        // arrange
        // a URL without a port must not pin the endpoint to the scheme default port.
        WriteSchemaSettings(
            """
            {
              "name": "Shop",
              "transports": {
                "http": {
                  "url": "http://localhost/graphql"
                }
              }
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Null(endpoint.Port);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_SkipComposition_When_SettingsFileIsMissing()
    {
        // arrange
        // no schema-settings.json is written
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var warning = Assert.Single(
            scope.LoggerFactory.Logger.Entries,
            entry => entry.Level == LogLevel.Warning);
        Assert.Equal(
            "Skipping GraphQL schema composition for shop: "
            + $"{IOPath.Combine(_appDirectory.FullName, "schema-settings.json")} not found.",
            warning.Message);
        Assert.Empty(app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        // the endpoint is declared regardless, so the app itself keeps running
        Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_DeclareEndpointWithoutPortPin_When_NoGatewayReferencesApp()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop",
              "transports": {
                "http": {
                  "url": "{{SHOP_URL}}/api/graphql"
                }
              },
              "environments": {
                "Aspire": {
                  "SHOP_URL": "http://localhost:5734"
                }
              }
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint();

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Null(endpoint.Port);
        Assert.Single(app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_RegisterAppAsRestartTrigger_When_GatewayComposesApp()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop"
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        // the composition watches the mapped resources for restarts
        var map = SchemaComposition.BuildSourceToGatewayMap([gateway.Resource], scope.Model);
        var (source, gateways) = Assert.Single(map);
        Assert.Same(app.Resource, source);
        Assert.Same(gateway.Resource, Assert.Single(gateways));
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_TargetDeclaredEndpoint_When_AppDeclaresCustomEndpointName()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop"
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithHttpEndpoint(port: 4321, name: "web")
            .WithGraphQLHttpEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal("web", endpoint.Name);
        var annotation = Assert.Single(
            app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Equal("web", annotation.EndpointName);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_TargetNamedEndpoint_When_EndpointNameIsProvided()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop"
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithHttpEndpoint(port: 1111, name: "admin")
            .WithHttpEndpoint(port: 2222, name: "api")
            .WithGraphQLHttpEndpoint(endpointName: "api");
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var annotation = Assert.Single(
            app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Equal("api", annotation.EndpointName);
        Assert.Equal(2, app.Resource.Annotations.OfType<EndpointAnnotation>().Count());
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_DeclareNamedEndpoint_When_EndpointNameHasNoEndpoint()
    {
        // arrange
        WriteSchemaSettings(
            """
            {
              "name": "Shop"
            }
            """);
        var builder = DistributedApplication.CreateBuilder();
        var app = builder
            .AddJavaScriptApp("shop", _appDirectory.FullName)
            .WithGraphQLHttpEndpoint(endpointName: "api");
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);

        // assert
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal("api", endpoint.Name);
        var annotation = Assert.Single(
            app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Equal("api", annotation.EndpointName);
    }

    /// <summary>
    /// Builds the distributed application without starting it and publishes
    /// <see cref="BeforeStartEvent"/>, so the built-in Aspire subscriptions and the extension
    /// under test run against the real application services.
    /// </summary>
    private static async Task<EventScope> PublishBeforeStartAsync(IDistributedApplicationBuilder builder)
    {
        // the built-in BeforeStartEvent subscriptions compute orchestrator resource names,
        // whose options demand these paths even though no orchestrator runs in the test
        builder.Configuration["DcpPublisher:CliPath"] = "dcp";
        builder.Configuration["DcpPublisher:DashboardPath"] = "dashboard";

        var loggerFactory = new RecordingLoggerFactory();
        var host = builder.Build();
        var model = host.Services.GetRequiredService<DistributedApplicationModel>();
        var services = new FallbackServiceProvider(
            new ServiceCollection()
                .AddSingleton<ILoggerFactory>(loggerFactory)
                .BuildServiceProvider(),
            host.Services);

        await builder.Eventing.PublishAsync(
            new BeforeStartEvent(services, model),
            TestContext.Current.CancellationToken);

        return new EventScope(host, model, services, loggerFactory);
    }

    private void WriteSchemaSettings(string json)
        => File.WriteAllText(IOPath.Combine(_appDirectory.FullName, "schema-settings.json"), json);

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => IOPath.Combine(
            IOPath.GetDirectoryName(sourceFile)!,
            "HotChocolate.Fusion.Aspire.Tests.csproj");

    public void Dispose()
        => _appDirectory.Delete(recursive: true);

    private sealed record EventScope(
        DistributedApplication Host,
        DistributedApplicationModel Model,
        IServiceProvider Services,
        RecordingLoggerFactory LoggerFactory) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
            => await Host.DisposeAsync();
    }

    private sealed class FallbackServiceProvider(
        IServiceProvider primary,
        IServiceProvider fallback) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => primary.GetService(serviceType) ?? fallback.GetService(serviceType);
    }

    private sealed class RecordingLoggerFactory : ILoggerFactory
    {
        public RecordingLogger<object> Logger { get; } = new();

        public ILogger CreateLogger(string categoryName) => Logger;

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}
