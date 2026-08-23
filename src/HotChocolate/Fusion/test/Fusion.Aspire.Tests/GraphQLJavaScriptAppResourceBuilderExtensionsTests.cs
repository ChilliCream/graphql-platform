using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

public sealed class GraphQLJavaScriptAppResourceBuilderExtensionsTests : IDisposable
{
    private static readonly TimeSpan s_waitTimeout = TimeSpan.FromSeconds(30);

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
        // the JavaScript overload attaches the source schema annotation to the schema anchor
        // instead of the app
        Assert.Empty(app.Resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_AttachSchemaAnchor_When_GatewayStarts()
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
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);
        await using var scope = await PublishBeforeStartAsync(builder);

        // act
        var (anchor, exception) = await ActivateAnchorAsync(builder, gateway.Resource, app.Resource, scope);

        // assert
        var annotation = Assert.Single(anchor.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Equal(
            IOPath.Combine(_appDirectory.FullName, "shop-schema.csproj"),
            anchor.Annotations.OfType<IProjectMetadata>().Single().ProjectPath);
        $"""
         GraphQLPath: {annotation.GraphQLPath}
         SchemaPath: {annotation.SchemaPath}
         EndpointPort: {app.Resource.Annotations.OfType<EndpointAnnotation>().Single().Port}
         SharesAppEndpoints: {anchor.Annotations.OfType<EndpointAnnotation>().SequenceEqual(
             app.Resource.Annotations.OfType<EndpointAnnotation>())}
         GatewayReferencesAnchor: {gateway.Resource.Annotations.OfType<ResourceRelationshipAnnotation>().Any(
             relationship => ReferenceEquals(relationship.Resource, anchor))}
         Error: {exception.Message}
         """.MatchInlineSnapshot(
            """
            GraphQLPath: /graphql
            SchemaPath: /graphql/schema.graphql
            EndpointPort: 6123
            SharesAppEndpoints: True
            GatewayReferencesAnchor: True
            Error: The source schema resource 'shop' required by 'gateway' did not become healthy.
            """);
    }

    [Fact]
    public async Task WithGraphQLHttpEndpoint_Should_PreserveConfiguration_When_ArgumentsAreProvided()
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
            .WithHttpEndpoint(port: 4321)
            .WithGraphQLHttpEndpoint(
                path: "/api/graphql",
                schemaPath: "/api/schema.graphql",
                sourceSchemaName: "Shop");
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);
        await using var scope = await PublishBeforeStartAsync(builder);

        // act
        var (anchor, _) = await ActivateAnchorAsync(builder, gateway.Resource, app.Resource, scope);

        // assert
        // the endpoint the app declares itself stays untouched
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(4321, endpoint.Port);
        var annotation = Assert.Single(anchor.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
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
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);
        await using var scope = await PublishBeforeStartAsync(builder);

        // act
        var (anchor, _) = await ActivateAnchorAsync(builder, gateway.Resource, app.Resource, scope);

        // assert
        var annotation = Assert.Single(anchor.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
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
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);
        await using var scope = await PublishBeforeStartAsync(builder);

        // act
        var (anchor, _) = await ActivateAnchorAsync(builder, gateway.Resource, app.Resource, scope);

        // assert
        var annotation = Assert.Single(anchor.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        $"""
         GraphQLPath: {annotation.GraphQLPath}
         SchemaPath: {annotation.SchemaPath}
         EndpointPort: {endpoint.Port?.ToString() ?? "none"}
         """.MatchInlineSnapshot(
            """
            GraphQLPath: /graphql
            SchemaPath: /graphql/schema.graphql
            EndpointPort: none
            """);
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
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);

        // act
        await using var scope = await PublishBeforeStartAsync(builder);
        await builder.Eventing.PublishAsync(
            new BeforeResourceStartedEvent(gateway.Resource, scope.Services),
            TestContext.Current.CancellationToken);

        // assert
        var warning = Assert.Single(
            scope.LoggerFactory.Logger.Entries,
            entry => entry.Level == LogLevel.Warning);
        Assert.Equal(
            "Skipping GraphQL schema composition for shop: "
            + $"{IOPath.Combine(_appDirectory.FullName, "schema-settings.json")} not found.",
            warning.Message);
        Assert.False(scope.Model.Resources.TryGetByName("shop-schema", out _));
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
        Assert.False(scope.Model.Resources.TryGetByName("shop-schema", out _));
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
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithNitroComposition()
            .WithReference(app);
        await using var scope = await PublishBeforeStartAsync(builder);

        // act
        var (anchor, _) = await ActivateAnchorAsync(builder, gateway.Resource, app.Resource, scope);

        // assert
        var endpoint = Assert.Single(app.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal("web", endpoint.Name);
        var annotation = Assert.Single(anchor.Annotations.OfType<GraphQLSourceSchemaAnnotation>());
        Assert.Equal("web", annotation.EndpointName);
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
        var notifications = host.Services.GetRequiredService<ResourceNotificationService>();
        var services = new FallbackServiceProvider(
            new ServiceCollection()
                .AddSingleton<ILoggerFactory>(loggerFactory)
                .BuildServiceProvider(),
            host.Services);

        await builder.Eventing.PublishAsync(
            new BeforeStartEvent(services, model),
            TestContext.Current.CancellationToken);

        return new EventScope(host, model, notifications, services, loggerFactory);
    }

    /// <summary>
    /// Drives the anchor activation that runs when a gateway is about to start. Aspire only
    /// completes its health wait under a running orchestrator, so the app is failed instead,
    /// which activates the anchor and then fails the wait with a deterministic error.
    /// </summary>
    private static async Task<(ProjectResource Anchor, DistributedApplicationException Exception)>
        ActivateAnchorAsync(
            IDistributedApplicationBuilder builder,
            IResource gateway,
            IResource app,
            EventScope scope)
    {
        await scope.Notifications.PublishUpdateAsync(
            app,
            snapshot => snapshot with { State = KnownResourceStates.FailedToStart });

        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            () => builder.Eventing
                .PublishAsync(
                    new BeforeResourceStartedEvent(gateway, scope.Services),
                    TestContext.Current.CancellationToken)
                .WaitAsync(s_waitTimeout, TestContext.Current.CancellationToken));

        var anchor = Assert.IsType<ProjectResource>(
            scope.Model.Resources.Single(resource => resource.Name == $"{app.Name}-schema"));

        return (anchor, exception);
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
        ResourceNotificationService Notifications,
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
