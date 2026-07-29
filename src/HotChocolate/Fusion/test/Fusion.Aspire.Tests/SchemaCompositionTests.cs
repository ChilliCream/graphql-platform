using System.Net;
using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        var logger = new RecordingLogger<SchemaComposition>();
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            logger);
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
        var schema = await composition.FetchSchemaFromEndpointAsync(
            "Products",
            new Uri(secretUrl),
            SchemaEndpointProtocol.GraphQL,
            client,
            maxRetries: 2,
            retryDelay: TimeSpan.Zero,
            retryTransientFailures: false,
            TestContext.Current.CancellationToken);

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            logger.Entries
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
        var logger = new RecordingLogger<SchemaComposition>();
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            logger);
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;
                cancellation.Cancel();
                throw new OperationCanceledException(cancellation.Token);
            }));

        // act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => composition.FetchSchemaFromEndpointAsync(
                "Products",
                new Uri("https://products.example.com/graphql"),
                SchemaEndpointProtocol.GraphQL,
                client,
                maxRetries: 2,
                retryDelay: TimeSpan.Zero,
                retryTransientFailures: false,
                cancellation.Token));

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            logger.Entries
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
    public async Task FetchSchemaFromEndpointAsync_Should_RetryServerErrorResponse_When_TransientRetryIsEnabled()
    {
        // arrange
        var attempts = 0;
        var logger = new RecordingLogger<SchemaComposition>();
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            logger);
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
        var schema = await composition.FetchSchemaFromEndpointAsync(
            "Products",
            new Uri("https://products.example.com/graphql"),
            SchemaEndpointProtocol.GraphQL,
            client,
            maxRetries: 3,
            retryDelay: TimeSpan.Zero,
            retryTransientFailures: true,
            TestContext.Current.CancellationToken);

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            logger.Entries
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
    public async Task FetchSchemaFromEndpointAsync_Should_RetryServerErrorException_When_TransientRetryIsEnabled()
    {
        // arrange
        var attempts = 0;
        var logger = new RecordingLogger<SchemaComposition>();
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            logger);
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
        var schema = await composition.FetchSchemaFromEndpointAsync(
            "Products",
            new Uri("https://products.example.com/graphql"),
            SchemaEndpointProtocol.GraphQL,
            client,
            maxRetries: 2,
            retryDelay: TimeSpan.Zero,
            retryTransientFailures: true,
            TestContext.Current.CancellationToken);

        // assert
        var debugLog = string.Join(
            Environment.NewLine,
            logger.Entries
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
    public async Task FetchSchemaFromEndpointAsync_Should_FailImmediately_When_TransientRetryIsDisabled()
    {
        // arrange
        var attempts = 0;
        var logger = new RecordingLogger<SchemaComposition>();
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            logger);
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }));

        // act
        var exception = await Assert.ThrowsAsync<SchemaFetchRequestException>(
            () => composition.FetchSchemaFromEndpointAsync(
                "Products",
                new Uri("https://products.example.com/graphql"),
                SchemaEndpointProtocol.GraphQL,
                client,
                maxRetries: 3,
                retryDelay: TimeSpan.Zero,
                retryTransientFailures: false,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(1, attempts);
        Assert.Equal(
            "Source schema 'Products' returned HTTP 503 (Service Unavailable) while downloading its schema.",
            exception.Message);
    }

    [Fact]
    public async Task FetchSchemaFromEndpointAsync_Should_FailImmediately_When_ErrorIsNotTransient()
    {
        // arrange
        var attempts = 0;
        var logger = new RecordingLogger<SchemaComposition>();
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            logger);
        using var client = new HttpClient(
            new StubHttpMessageHandler(_ =>
            {
                attempts++;
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }));

        // act
        var exception = await Assert.ThrowsAsync<SchemaFetchRequestException>(
            () => composition.FetchSchemaFromEndpointAsync(
                "Products",
                new Uri("https://products.example.com/graphql"),
                SchemaEndpointProtocol.GraphQL,
                client,
                maxRetries: 3,
                retryDelay: TimeSpan.Zero,
                retryTransientFailures: true,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(1, attempts);
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task DiscoverReferencedSourceSchemasAsync_Should_Throw_When_RecompositionSourceIsUnavailable()
    {
        // arrange
        // the products project has no schema-settings.json, so its source schema cannot be loaded.
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
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            new RecordingLogger<SchemaComposition>());

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => composition.DiscoverReferencedSourceSchemasAsync(
                gatewayResource,
                model,
                isRecomposition: true,
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
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            new RecordingLogger<SchemaComposition>());

        // act
        await composition.CopyArchiveWithRetryAsync(
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
        var composition = new SchemaComposition(
            new TestHostApplicationLifetime(),
            new RecordingLogger<SchemaComposition>());

        // act
        var exception = await Assert.ThrowsAsync<IOException>(
            () => composition.CopyArchiveWithRetryAsync(
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

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(sourceFile)!,
            "HotChocolate.Fusion.Aspire.Tests.csproj");

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
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }
}
