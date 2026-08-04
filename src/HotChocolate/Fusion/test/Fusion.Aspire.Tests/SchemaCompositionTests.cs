using System.Net;
using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

public sealed class SchemaCompositionTests
{
    [Fact]
    public void WithGraphQLSchemaComposition_Should_UseValidationAndOutput_WhenArgumentsAreProvided()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition(
                disableValidation: true,
                outputFileName: "custom.far");

        // assert
        var annotation = Assert.Single(
            gateway.Resource.Annotations.OfType<GraphQLSchemaCompositionAnnotation>());
        $"""
        Disable validation: {annotation.Settings.DisableSchemaValidation}
        Output: {annotation.OutputFileName}
        """.MatchInlineSnapshot(
            """
            Disable validation: True
            Output: custom.far
            """);
    }

    [Fact]
    public void WithGraphQLSchemaComposition_Should_UseDefaultOutput_WhenSettingsAreProvided()
    {
        // arrange
        var builder = DistributedApplication.CreateBuilder();
        var settings = new GraphQLCompositionSettings
        {
            EnableGlobalObjectIdentification = true
        };

        // act
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition(settings);

        // assert
        var annotation = Assert.Single(
            gateway.Resource.Annotations.OfType<GraphQLSchemaCompositionAnnotation>());
        $"""
        Global object identification: {annotation.Settings.EnableGlobalObjectIdentification}
        Output: {annotation.OutputFileName}
        """.MatchInlineSnapshot(
            """
            Global object identification: True
            Output: gateway.far
            """);
    }

    [Fact]
    public async Task ExecuteRecomposeCommandAsync_Should_Coalesce_When_CompositionIsInProgress()
    {
        // arrange
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition();
        var model = new DistributedApplicationModel(builder.Resources);
        using var gate = new SemaphoreSlim(0, 1);

        // act
        var result = await harness.Composition.ExecuteRecomposeCommandAsync(
            gateway.Resource,
            model,
            gate,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(result.Success);
        Assert.Equal("Composition already in progress", result.Message);
        Assert.Equal(0, gate.CurrentCount);
    }

    [Fact]
    public async Task ExecuteRecomposeCommandAsync_Should_ReturnCanceled_When_CommandIsAlreadyCanceled()
    {
        // arrange
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition();
        var model = new DistributedApplicationModel(builder.Resources);
        using var gate = new SemaphoreSlim(1, 1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // act
        var result = await harness.Composition.ExecuteRecomposeCommandAsync(
            gateway.Resource,
            model,
            gate,
            cancellation.Token);

        // assert
        $"""
        Success: {result.Success}
        Canceled: {result.Canceled}
        Gate count: {gate.CurrentCount}
        """.MatchInlineSnapshot(
            """
            Success: False
            Canceled: True
            Gate count: 1
            """);
    }

    [Fact]
    public async Task ExecuteRecomposeCommandAsync_Should_ReturnFailure_When_CompositionFails()
    {
        // arrange
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
#pragma warning disable CS0618 // The file based source schema API is under test.
        var source = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaFile("missing.graphql");
#pragma warning restore CS0618
        var gateway = builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(source);
        var model = new DistributedApplicationModel(builder.Resources);
        using var gate = new SemaphoreSlim(1, 1);

        // act
        var result = await harness.Composition.ExecuteRecomposeCommandAsync(
            gateway.Resource,
            model,
            gate,
            TestContext.Current.CancellationToken);

        // assert
        Assert.False(result.Success);
        Assert.Equal("Schema composition failed for 'gateway'.", result.Message);
        Assert.Equal(1, gate.CurrentCount);
    }

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
            .WithGraphQLHttpEndpoint();
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
#pragma warning disable CS0618 // The file based source schema API is under test.
            var products = builder
                .AddProject("products", sourceProjectFile)
                .WithGraphQLSchemaFile();
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // The file based source schema API is under test.
        var products = builder
            .AddProject("products", GetTestProjectFile())
            .WithGraphQLSchemaFile();
#pragma warning restore CS0618
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
            .WithGraphQLHttpEndpoint();
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
#pragma warning disable CS0618 // The file based source schema API is under test.
        var orders = builder
            .AddProject("orders", GetTestProjectFile())
            .WithGraphQLSchemaFile();
#pragma warning restore CS0618
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
            .WithGraphQLHttpEndpoint();
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
            .WithGraphQLHttpEndpoint();
#pragma warning disable CS0618 // The file based source schema API is under test.
        var orders = builder
            .AddProject("orders", GetTestProjectFile())
            .WithGraphQLSchemaFile();
#pragma warning restore CS0618
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

    [Fact]
    public async Task DiscoverReferencedSourceSchemasAsync_Should_DownloadSchemaThroughGraphQLRoute_When_SourceSchemaUsesApolloFederation()
    {
        // arrange
        // an Apollo Federation source schema serves its schema through the GraphQL route, so the
        // schema document path of the annotation never applies.
        await using var server = await SchemaEndpointServer.StartAsync(
            "/api/graphql",
            """{"data":{"_service":{"sdl":"type Query { product: String }"}}}""");
        using var project = new TempSourceSchemaProject(
            """
            {
              "name": "products",
              "extensions": {
                "chillicream": {
                  "apolloFederationSupport": { "version": "2.0" }
                }
              }
            }
            """);
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", project.ProjectFile)
            .WithHttpEndpoint(name: "http")
            .WithGraphQLHttpEndpoint(path: "/api/graphql");
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(products);
        var model = new DistributedApplicationModel(builder.Resources);
        var gatewayResource = model.GetGraphQLCompositionResources().Single();
        products.Resource.AllocateHttpEndpoint(server.Port);

        // act
        var sourceSchemas = await harness.Composition.DiscoverReferencedSourceSchemasAsync(
            gatewayResource,
            model,
            TestContext.Current.CancellationToken);

        // assert
        try
        {
            var sourceSchema = Assert.Single(sourceSchemas);
            Assert.Equal(
                new Uri($"http://127.0.0.1:{server.Port}/api/graphql"),
                sourceSchema.HttpEndpointUrl);
            Assert.Equal("type Query { product: String }", sourceSchema.Schema.SourceText);
            Assert.Equal(new[] { "/api/graphql" }, server.RequestedPaths);
        }
        finally
        {
            foreach (var sourceSchema in sourceSchemas)
            {
                sourceSchema.SchemaSettings.Dispose();
            }
        }
    }

    [Fact]
    public async Task DiscoverReferencedSourceSchemasAsync_Should_DownloadSchemaFromSchemaPath_When_SourceSchemaDoesNotUseApolloFederation()
    {
        // arrange
        await using var server = await SchemaEndpointServer.StartAsync(
            "/api/schema.graphql",
            "type Query { product: String }");
        using var project = new TempSourceSchemaProject("""{ "name": "products" }""");
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", project.ProjectFile)
            .WithHttpEndpoint(name: "http")
            .WithGraphQLHttpEndpoint(path: "/api/graphql", schemaPath: "/api/schema.graphql");
        builder
            .AddProject("gateway", GetTestProjectFile())
            .WithGraphQLSchemaComposition()
            .WithReference(products);
        var model = new DistributedApplicationModel(builder.Resources);
        var gatewayResource = model.GetGraphQLCompositionResources().Single();
        products.Resource.AllocateHttpEndpoint(server.Port);

        // act
        var sourceSchemas = await harness.Composition.DiscoverReferencedSourceSchemasAsync(
            gatewayResource,
            model,
            TestContext.Current.CancellationToken);

        // assert
        try
        {
            var sourceSchema = Assert.Single(sourceSchemas);
            Assert.Equal(
                new Uri($"http://127.0.0.1:{server.Port}/api/schema.graphql"),
                sourceSchema.HttpEndpointUrl);
            Assert.Equal(new[] { "/api/schema.graphql" }, server.RequestedPaths);
        }
        finally
        {
            foreach (var sourceSchema in sourceSchemas)
            {
                sourceSchema.SchemaSettings.Dispose();
            }
        }
    }

    [Fact]
    public async Task DiscoverReferencedSourceSchemasAsync_Should_Fail_When_ASourceSchemaWithoutFederationDeclaresNoSchemaPath()
    {
        // arrange
        // no schema endpoint stands by, so the discovery must fail before it fetches anything.
        using var project = new TempSourceSchemaProject("""{ "name": "products" }""");
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", project.ProjectFile)
            .WithHttpEndpoint(name: "http")
            .WithGraphQLHttpEndpoint(path: "/api/graphql", schemaPath: null);
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
        $"""
        Exception: {exception.Message}
        Errors:
        {DescribeErrors(harness)}
        """.MatchInlineSnapshot(
            """
            Exception: The source schema for resource 'products' could not be loaded.
            Errors:
            The source schema products of the resource products does not use Apollo Federation and declares no schema document path. Pass a schemaPath to WithGraphQLHttpEndpoint.
            """);
    }

    [Fact]
    public async Task DiscoverReferencedSourceSchemasAsync_Should_Fail_When_TheAnnotationDeclaresNoGraphQLRoute()
    {
        // arrange
        // a retired annotation declares no GraphQL route, so the resource has to be migrated.
        using var project = new TempSourceSchemaProject("""{ "name": "products" }""");
        var harness = CreateHarness();
        var builder = DistributedApplication.CreateBuilder();
#pragma warning disable CS0618 // The retired schema endpoint API is under test.
        var products = builder
            .AddProject("products", project.ProjectFile)
            .WithHttpEndpoint(name: "http")
            .WithGraphQLSchemaEndpoint();
#pragma warning restore CS0618
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
        $"""
        Exception: {exception.Message}
        Errors:
        {DescribeErrors(harness)}
        """.MatchInlineSnapshot(
            """
            Exception: The source schema for resource 'products' could not be loaded.
            Errors:
            The source schema products of the resource products does not declare the path of its GraphQL endpoint. Call WithGraphQLHttpEndpoint on the resource.
            """);
    }

    private static string DescribeErrors(CompositionHarness harness)
        => string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level is LogLevel.Error)
                .Select(entry => entry.Message));

    private static CompositionHarness CreateHarness() => CompositionHarness.Create(coordinator: null);

    private static string GetTestProjectFile([CallerFilePath] string sourceFile = "")
        => IOPath.Combine(
            IOPath.GetDirectoryName(sourceFile)!,
            "HotChocolate.Fusion.Aspire.Tests.csproj");

    /// <summary>
    /// A project directory with a project file and a schema-settings.json, which is what an
    /// endpoint-based source schema resource needs on disk.
    /// </summary>
    private sealed class TempSourceSchemaProject : IDisposable
    {
        private readonly DirectoryInfo _directory;

        public TempSourceSchemaProject(string schemaSettingsJson)
        {
            _directory = Directory.CreateTempSubdirectory();
            ProjectFile = IOPath.Combine(_directory.FullName, "products.csproj");
            File.WriteAllText(ProjectFile, "<Project />");
            File.WriteAllText(
                IOPath.Combine(_directory.FullName, "schema-settings.json"),
                schemaSettingsJson);
        }

        public string ProjectFile { get; }

        public void Dispose() => _directory.Delete(recursive: true);
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
}
