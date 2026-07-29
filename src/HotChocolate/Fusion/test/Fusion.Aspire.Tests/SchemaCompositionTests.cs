using System.Net;
using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

public sealed class SchemaCompositionTests
{
    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_RetryWithoutLeakingEndpoint_When_ResponseReadFails()
    {
        // arrange
        const string secretUrl =
            "https://user:secret@products.example.com/graphql?token=secret";
        var attempts = 0;
        var harness = CreateHarness();
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;

                return attempts == 1
                    ? new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StreamContent(new FailingReadStream(secretUrl))
                    }
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("type Query { hello: String }")
                    };
            }));

        // act
        var schema = await harness.Composition.FetchSchemaFromEndpointAsync(
            "Products",
            new Uri(secretUrl),
            SchemaEndpointProtocol.GraphQL,
            client,
            maxRetries: 2,
            retryDelay: TimeSpan.Zero,
            TestContext.Current.CancellationToken);

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Debug)
                .Select(entry =>
                    $"{entry.Message} | Exception: {entry.Exception?.Message ?? "<none>"}"));

        $$"""
        Schema: {{schema}}
        Attempts: {{attempts}}
        Debug:
        {{debugLog}}
        """.MatchInlineSnapshot(
            """
            Schema: type Query { hello: String }
            Attempts: 2
            Debug:
            Waiting for schema service Products | Exception: <none>
            Schema service Products was unavailable (attempt 1/2) | Exception: <none>
            """);
    }

    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_PreserveCallerCancellation_When_RequestIsCanceled()
    {
        // arrange
        var attempts = 0;
        using var cancellation = new CancellationTokenSource();
        var harness = CreateHarness();
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }));

        // act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => harness.Composition.FetchSchemaFromEndpointAsync(
                "Products",
                new Uri("https://products.example.com/graphql"),
                SchemaEndpointProtocol.GraphQL,
                client,
                maxRetries: 2,
                retryDelay: TimeSpan.Zero,
                cancellation.Token));

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Debug)
                .Select(entry => entry.Message));

        $$"""
        Attempts: {{attempts}}
        Caller cancellation preserved: {{exception.CancellationToken == cancellation.Token}}
        Debug:
        {{debugLog}}
        """.MatchInlineSnapshot(
            """
            Attempts: 1
            Caller cancellation preserved: True
            Debug:
            Waiting for schema service Products
            """);
    }

    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_RetryServerErrorResponse_When_EndpointReturnsServerError()
    {
        // arrange
        var attempts = 0;
        var harness = CreateHarness();
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;

                return attempts < 3
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("type Query { hello: String }")
                    };
            }));

        // act
        var schema = await harness.Composition.FetchSchemaFromEndpointAsync(
            "Products",
            new Uri("https://products.example.com/graphql"),
            SchemaEndpointProtocol.GraphQL,
            client,
            maxRetries: 3,
            retryDelay: TimeSpan.Zero,
            TestContext.Current.CancellationToken);

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Debug)
                .Select(entry => entry.Message));

        $$"""
        Schema: {{schema}}
        Attempts: {{attempts}}
        Debug:
        {{debugLog}}
        """.MatchInlineSnapshot(
            """
            Schema: type Query { hello: String }
            Attempts: 3
            Debug:
            Waiting for schema service Products
            Schema service Products returned a transient server error (attempt 1/3)
            Schema service Products returned a transient server error (attempt 2/3)
            """);
    }

    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_RetryServerErrorException_When_ProxyThrowsServerError()
    {
        // arrange
        var attempts = 0;
        var harness = CreateHarness();
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;

                if (attempts == 1)
                {
                    throw new HttpRequestException(
                        "The proxy rejected the request.",
                        inner: null,
                        HttpStatusCode.BadGateway);
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("type Query { hello: String }")
                };
            }));

        // act
        var schema = await harness.Composition.FetchSchemaFromEndpointAsync(
            "Products",
            new Uri("https://products.example.com/graphql"),
            SchemaEndpointProtocol.GraphQL,
            client,
            maxRetries: 2,
            retryDelay: TimeSpan.Zero,
            TestContext.Current.CancellationToken);

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Debug)
                .Select(entry => entry.Message));

        $$"""
        Schema: {{schema}}
        Attempts: {{attempts}}
        Debug:
        {{debugLog}}
        """.MatchInlineSnapshot(
            """
            Schema: type Query { hello: String }
            Attempts: 2
            Debug:
            Waiting for schema service Products
            Schema service Products returned a transient server error (attempt 1/2)
            """);
    }

    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_ReturnNull_When_RetryBudgetIsExhausted()
    {
        // arrange
        var attempts = 0;
        var harness = CreateHarness();
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }));

        // act
        var schema = await harness.Composition.FetchSchemaFromEndpointAsync(
            "Products",
            new Uri("https://products.example.com/graphql"),
            SchemaEndpointProtocol.GraphQL,
            client,
            maxRetries: 2,
            retryDelay: TimeSpan.Zero,
            TestContext.Current.CancellationToken);

        // assert
        var warningLog = string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Warning)
                .Select(entry => entry.Message));

        $$"""
        Schema: {{schema ?? "<null>"}}
        Attempts: {{attempts}}
        Warnings:
        {{warningLog}}
        """.MatchInlineSnapshot(
            """
            Schema: <null>
            Attempts: 2
            Warnings:
            Schema service Products failed to become ready after 2 attempts
            """);
    }

    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_FailImmediately_When_ErrorIsNotTransient()
    {
        // arrange
        var attempts = 0;
        var harness = CreateHarness();
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

        // act
        var exception = await Assert.ThrowsAsync<SchemaFetchRequestException>(
            () => harness.Composition.FetchSchemaFromEndpointAsync(
                "Products",
                new Uri("https://products.example.com/graphql"),
                SchemaEndpointProtocol.GraphQL,
                client,
                maxRetries: 3,
                retryDelay: TimeSpan.Zero,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(1, attempts);
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
        Assert.Equal(
            "Source schema 'Products' returned HTTP 404 (Not Found) while downloading its schema.",
            exception.Message);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_ThrowWithoutStoppingApplication_When_SourceFailsToStart()
    {
        // arrange
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(products);
        var model = new DistributedApplicationModel(builder.Resources);
        var gateway = model.GetGraphQLCompositionResources().Single();
        var productsResource = model.Resources.Single(r => r.Name == "products");
        using var compositionGate = new SemaphoreSlim(1, 1);
        await harness.Notifications.PublishUpdateAsync(
            productsResource,
            snapshot => snapshot with { State = KnownResourceStates.FailedToStart });

        // act
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            () => harness.Composition.ComposeOnGatewayStartAsync(
                gateway,
                model,
                compositionGate,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "The GraphQL schema composition for 'gateway' failed: The source schema resource "
            + "'products' required by 'gateway' did not become healthy.",
            exception.Message);
        Assert.Equal(0, harness.Lifetime.StopApplicationCalls);
        Assert.Equal(1, compositionGate.CurrentCount);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_ThrowWithoutStoppingApplication_When_CompositionFails()
    {
        // arrange
        // the source schema file contains invalid GraphQL, so the composition itself fails.
        var tempRoot = Directory.CreateTempSubdirectory("fusion-aspire-composition-");

        try
        {
            var sourceDirectory = Directory.CreateDirectory(IOPath.Combine(tempRoot.FullName, "products"));
            var gatewayDirectory = Directory.CreateDirectory(IOPath.Combine(tempRoot.FullName, "gateway"));
            var sourceProjectFile = IOPath.Combine(sourceDirectory.FullName, "products.csproj");
            var gatewayProjectFile = IOPath.Combine(gatewayDirectory.FullName, "gateway.csproj");
            await File.WriteAllTextAsync(
                sourceProjectFile, "<Project />", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(
                gatewayProjectFile, "<Project />", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(
                IOPath.Combine(sourceDirectory.FullName, "schema.graphqls"),
                "type Query {",
                TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(
                IOPath.Combine(sourceDirectory.FullName, "schema-settings.json"),
                """{ "name": "products" }""",
                TestContext.Current.CancellationToken);

            var harness = CreateHarness();
            var builder = DistributedApplication.CreateBuilder();
            var products = builder
                .AddProject("products", sourceProjectFile)
                .WithGraphQLSchemaFile();
            builder
                .AddProject("gateway", gatewayProjectFile)
                .WithGraphQLSchemaComposition()
                .WithReference(products);
            var model = new DistributedApplicationModel(builder.Resources);
            var gateway = model.GetGraphQLCompositionResources().Single();
            using var compositionGate = new SemaphoreSlim(1, 1);

            // act
            var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
                () => harness.Composition.ComposeOnGatewayStartAsync(
                    gateway,
                    model,
                    compositionGate,
                    TestContext.Current.CancellationToken));

            // assert
            Assert.Equal("The GraphQL schema composition for 'gateway' failed.", exception.Message);
            Assert.Equal(0, harness.Lifetime.StopApplicationCalls);
            Assert.Equal(1, compositionGate.CurrentCount);
        }
        finally
        {
            tempRoot.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_ThrowWithoutStoppingApplication_When_StartupSourceSchemaCannotBeLoaded()
    {
        // arrange
        // the products project declares a schema file that does not exist, so its source
        // schema cannot be loaded and the gateway must not start with a partial schema.
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaFile();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(products);
        var model = new DistributedApplicationModel(builder.Resources);
        var gateway = model.GetGraphQLCompositionResources().Single();
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            () => harness.Composition.ComposeOnGatewayStartAsync(
                gateway,
                model,
                compositionGate,
                TestContext.Current.CancellationToken));

        // assert
        var errorLog = string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Error)
                .Select(entry => entry.Message));

        $$"""
        Exception: {{exception.Message}}
        StopApplication calls: {{harness.Lifetime.StopApplicationCalls}}
        Gate count: {{compositionGate.CurrentCount}}
        Errors:
        {{errorLog}}
        """.MatchInlineSnapshot(
            """
            Exception: The GraphQL schema composition for 'gateway' failed.
            StopApplication calls: 0
            Gate count: 1
            Errors:
            Schema composition failed for gateway: The source schema for resource 'products' could not be loaded.
            The GraphQL schema composition for 'gateway' failed.
            """);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_WaitForRunningRecomposition_When_CompositionGateIsHeld()
    {
        // arrange
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        builder.AddProject("gateway", GetTestProjectFile());
        var model = new DistributedApplicationModel(builder.Resources);
        var gateway = model.Resources.OfType<IResourceWithEndpoints>().Single(r => r.Name == "gateway");
        using var compositionGate = new SemaphoreSlim(1, 1);
        await compositionGate.WaitAsync(TestContext.Current.CancellationToken);

        // act
        // the held gate simulates a recomposition that is still running for the gateway
        var startupComposition = harness.Composition.ComposeOnGatewayStartAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);
        var completedWhileGateHeld = startupComposition.IsCompleted;
        compositionGate.Release();
        await startupComposition;

        // assert
        Assert.False(completedWhileGateHeld);
        Assert.Equal(1, compositionGate.CurrentCount);
    }

    [Fact]
    public async Task RunGuardedRecompositionAsync_Should_WaitForStartupComposition_When_CompositionGateIsHeld()
    {
        // arrange
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        builder.AddProject("gateway", GetTestProjectFile());
        var model = new DistributedApplicationModel(builder.Resources);
        var gateway = model.Resources.OfType<IResourceWithEndpoints>().Single(r => r.Name == "gateway");
        using var compositionGate = new SemaphoreSlim(1, 1);
        await compositionGate.WaitAsync(TestContext.Current.CancellationToken);

        // act
        // the held gate simulates a startup composition that is still running for the gateway
        var recomposition = harness.Composition.RunGuardedRecompositionAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);
        var logCountWhileGateHeld = harness.Logger.Entries.Count;
        compositionGate.Release();
        await recomposition;

        // assert
        var infoLog = string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Information)
                .Select(entry => entry.Message));

        $$"""
        Log entries while the gate was held: {{logCountWhileGateHeld}}
        Gate count after the recomposition: {{compositionGate.CurrentCount}}
        Information:
        {{infoLog}}
        """.MatchInlineSnapshot(
            """
            Log entries while the gate was held: 0
            Gate count after the recomposition: 1
            Information:
            Recomposing GraphQL schema for gateway...
            Schema recomposition for gateway completed.
            """);
    }

    [Fact]
    public async Task WaitForSourceSchemaResourcesReadyAsync_Should_Throw_When_EndpointSourceIsUnavailable()
    {
        // arrange
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(products);
        var model = new DistributedApplicationModel(builder.Resources);
        var gateway = model.GetGraphQLCompositionResources().Single();
        var productsResource = model.Resources.Single(r => r.Name == "products");
        await harness.Notifications.PublishUpdateAsync(
            productsResource,
            snapshot => snapshot with { State = KnownResourceStates.FailedToStart });

        // act
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            () => harness.Composition.WaitForSourceSchemaResourcesReadyAsync(
                gateway,
                model,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "The source schema resource 'products' required by 'gateway' did not become healthy.",
            exception.Message);
        Assert.IsType<DistributedApplicationException>(exception.InnerException);
    }

    [Fact]
    public async Task WaitForSourceSchemaResourcesReadyAsync_Should_NotWait_When_SourceIsFileBased()
    {
        // arrange
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var orders = builder
            .AddProject("orders", GetTestProjectFile())
            .WithGraphQLSchemaFile();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(orders);
        var model = new DistributedApplicationModel(builder.Resources);
        var gateway = model.GetGraphQLCompositionResources().Single();
        // the notification service knows no resource states, so a regression that waits
        // would never finish. The timeout turns such a hang into a fast test failure.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // act
        await harness.Composition.WaitForSourceSchemaResourcesReadyAsync(
            gateway,
            model,
            timeout.Token);

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Debug)
                .Select(entry => entry.Message));

        Assert.Equal(string.Empty, debugLog);
    }

    [Fact]
    public async Task DiscoverReferencedSourceSchemasAsync_Should_Throw_When_SourceSchemaCannotBeLoaded()
    {
        // arrange
        // the products project has no schema-settings.json, so its source schema cannot be loaded.
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaEndpoint();
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(products);
        var model = new DistributedApplicationModel(builder.Resources);
        var gatewayResource = model.GetGraphQLCompositionResources().Single();

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Composition.DiscoverReferencedSourceSchemasAsync(
                gatewayResource,
                model,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "The source schema for resource 'products' could not be loaded.",
            exception.Message);
    }

    [Fact]
    public async Task CopyArchiveWithRetryAsync_Should_ReplaceArchive_When_CopyFailsTransiently()
    {
        // arrange
        var attempts = 0;
        var harness = CreateHarness();

        // act
        await harness.Composition.CopyArchiveWithRetryAsync(
            () =>
            {
                attempts++;

                if (attempts < 3)
                {
                    throw new IOException("The file is in use.");
                }
            },
            "gateway.far",
            maxAttempts: 5,
            retryDelay: TimeSpan.Zero,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task CopyArchiveWithRetryAsync_Should_Throw_When_CopyKeepsFailing()
    {
        // arrange
        var attempts = 0;
        var harness = CreateHarness();

        // act
        var exception = await Assert.ThrowsAsync<IOException>(
            () => harness.Composition.CopyArchiveWithRetryAsync(
                () =>
                {
                    attempts++;
                    throw new IOException("The file is in use.");
                },
                "gateway.far",
                maxAttempts: 3,
                retryDelay: TimeSpan.Zero,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(3, attempts);
        Assert.Equal("The file is in use.", exception.Message);
    }

    [Fact]
    public void BuildSourceToGatewayMap_Should_MapSourceResourcesToGateways_When_GatewaysShareSources()
    {
        // arrange
        // telemetry is referenced but has no source schema annotation and must not appear.
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaEndpoint();
        var orders = builder
            .AddProject("orders", GetTestProjectFile())
            .WithGraphQLSchemaFile();
        var telemetry = builder.AddProject("telemetry", GetTestProjectFile());
        builder
            .AddProject("gateway1", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(products)
            .WithReference(orders)
            .WithReference(telemetry);
        builder
            .AddProject("gateway2", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(orders);
        var model = new DistributedApplicationModel(builder.Resources);
        var gateways = model.GetGraphQLCompositionResources().ToList();

        // act
        var map = SchemaComposition.BuildSourceToGatewayMap(gateways, model);

        // assert
        var description = string.Join(
            Environment.NewLine,
            map
                .OrderBy(entry => entry.Key.Name, StringComparer.Ordinal)
                .Select(entry =>
                    $"{entry.Key.Name} -> {string.Join(", ", entry.Value.Select(gateway => gateway.Name))}"));

        description.MatchInlineSnapshot(
            """
            orders -> gateway1, gateway2
            products -> gateway1
            """);
    }

    private static CompositionHarness CreateHarness()
    {
        var logger = new RecordingLogger<SchemaComposition>();
        var lifetime = new TestHostApplicationLifetime();
        var resourceLoggerService = new ResourceLoggerService();
        var notifications = new ResourceNotificationService(
            new RecordingLogger<ResourceNotificationService>(),
            lifetime,
            EmptyServiceProvider.Instance,
            resourceLoggerService);
        var composition = new SchemaComposition(
            notifications,
            resourceLoggerService,
            lifetime,
            logger);

        return new CompositionHarness(composition, notifications, logger, lifetime);
    }

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => IOPath.Combine(
            IOPath.GetDirectoryName(sourceFile)!,
            "HotChocolate.Fusion.Aspire.Tests.csproj");

    private sealed record CompositionHarness(
        SchemaComposition Composition,
        ResourceNotificationService Notifications,
        RecordingLogger<SchemaComposition> Logger,
        TestHostApplicationLifetime Lifetime);

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }

    private sealed class FailingReadStream(string message) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => throw new IOException(message);

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
            => Task.FromException<int>(new IOException(message));

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            => ValueTask.FromException<int>(new IOException(message));

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime
    {
        public int StopApplicationCalls { get; private set; }

        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication() => StopApplicationCalls++;
    }
}
