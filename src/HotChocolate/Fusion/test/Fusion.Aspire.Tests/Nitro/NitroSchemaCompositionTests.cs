using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// Drives the start gate and the recomposition of a gateway that composes against a fusion
/// configuration of Nitro, with file-based source schemas and a Nitro stand-in on a loopback port.
/// </summary>
/// <remarks>
/// Two behaviors cannot be observed without an application host and are covered at their seams
/// instead: the URL that an allocated endpoint injects into the composed configuration (the
/// resources of this harness never allocate an endpoint, which is what
/// <c>AspireCompositionHelperTests.BuildLocalUrlOverrides_*</c> covers) and the fact that the
/// download and the wait for the source schema resources run at the same time (the wait needs
/// resource health that this harness cannot report).
/// </remarks>
public sealed class NitroSchemaCompositionTests : IAsyncLifetime
{
    private const string Stage = "production";
    private const string GatewayApiId = "QXBpCmdhdGV3YXk";
    private const string SubgraphApiId = "QXBpCnByb2R1Y3Rz";
    private const string ProductsSchema = "type Query { product: String }";
    private static readonly DateTimeOffset s_now = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly NitroTestDirectory _directory = new();
    private readonly FakeTimeProvider _timeProvider = new(s_now);
    private readonly HttpClient _httpClient = new();
    private string _productsProjectFile = null!;
    private string _gatewayProjectFile = null!;
    private string _gatewayArchivePath = null!;
    private FakeNitroServer _server = null!;

    public async ValueTask InitializeAsync()
    {
        _server = await FakeNitroServer.StartAsync();

        var productsDirectory = Directory.CreateDirectory(_directory.GetPath("products"));
        var gatewayDirectory = Directory.CreateDirectory(_directory.GetPath("gateway"));

        _productsProjectFile = IOPath.Combine(productsDirectory.FullName, "products.csproj");
        _gatewayProjectFile = IOPath.Combine(gatewayDirectory.FullName, "gateway.csproj");
        _gatewayArchivePath = IOPath.Combine(gatewayDirectory.FullName, "gateway.far");

        await File.WriteAllTextAsync(_productsProjectFile, "<Project />");
        await File.WriteAllTextAsync(_gatewayProjectFile, "<Project />");
        await File.WriteAllTextAsync(
            IOPath.Combine(productsDirectory.FullName, "schema.graphqls"),
            ProductsSchema);
        await File.WriteAllTextAsync(
            IOPath.Combine(productsDirectory.FullName, "schema-settings.json"),
            """
            {
              "name": "products",
              "transports": {
                "http": {
                  "url": "https://products.example.com/graphql",
                  "devUrl": "{{PRODUCTS_DEV_URL}}"
                }
              },
              "environments": {
                "Aspire": {
                  "PRODUCTS_DEV_URL": "http://localhost:5001/graphql"
                },
                "production": {
                  "PRODUCTS_DEV_URL": "https://products.production.example.com/graphql"
                }
              }
            }
            """);
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        _directory.Dispose();
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_ComposeTheLocalSchemaOntoTheSeed_When_TheGatewaySelectsAnApi()
    {
        // arrange
        // the seed carries an outdated products schema, so the local products schema has to
        // replace it, and it carries two schemas that only exist in Nitro.
        await ServeSeedAsync();
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(GatewayApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        await harness.Composition.ComposeOnGatewayStartAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // assert
        (await DescribeArchiveAsync(TestContext.Current.CancellationToken)).MatchInlineSnapshot(
            """
            Source schemas: orders, products, reviews
            products document: type Query { product: String }
            Gateway settings:
            {
              "sourceSchemas": {
                "products": {
                  "transports": {
                    "http": {
                      "url": "http://localhost:5001/graphql"
                    }
                  }
                },
                "orders": {
                  "transports": {
                    "http": {
                      "url": "https://orders.example.com/graphql"
                    }
                  }
                },
                "reviews": {
                  "transports": {
                    "http": {
                      "url": "https://reviews.dev.example.com/graphql"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_DropSchemas_When_TheyAreOnlyInThePreviousOutput()
    {
        // arrange
        // a schema that was composed into the previous output but is neither in the seed nor in
        // the distributed application has to disappear.
        await ServeSeedAsync();
        await File.WriteAllBytesAsync(
            _gatewayArchivePath,
            await NitroTestArchive.CreateAsync(
                TestContext.Current.CancellationToken,
                new NitroTestSourceSchema(
                    "legacy",
                    "type Query { legacy: String }",
                    CreateSettings("legacy", "https://legacy.example.com/graphql"))),
            TestContext.Current.CancellationToken);
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(GatewayApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        await harness.Composition.ComposeOnGatewayStartAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            ["orders", "products", "reviews"],
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                _gatewayArchivePath,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_NotContactNitro_When_TheGatewaySelectsNoApi()
    {
        // arrange
        await ServeSeedAsync();
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(gatewayApiId: null);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        await harness.Composition.ComposeOnGatewayStartAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // assert
        $"""
        Requests: {_server.Requests.Count}
        Source schemas: {await ReadSourceSchemaNamesAsync()}
        """.MatchInlineSnapshot(
            """
            Requests: 0
            Source schemas: products
            """);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_ComposeFromTheCache_When_TheDownloadFails()
    {
        // arrange
        await PrimeTheCacheAsync();
        _server.DownloadHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status503ServiceUnavailable);
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(GatewayApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        await harness.Composition.ComposeOnGatewayStartAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // assert
        $"""
        Source schemas: {await ReadSourceSchemaNamesAsync()}
        Warnings:
        {DescribeWarnings(harness)}
        """.MatchInlineSnapshot(
            """
            Source schemas: orders, products, reviews
            Warnings:
            Attempt 1 of 1 to download the fusion configuration for the api QXBpCmdhdGV3YXk and the stage production from https://nitro.test/api/v1/apis/QXBpCmdhdGV3YXk/fusion/configurations/latest/download?stage=production&format=far&fusionVersion=2.0.0 failed. Nitro returned the status code 503.
            A fresh fusion configuration could NOT be fetched from Nitro. The fusion configuration for the api 'QXBpCmdhdGV3YXk' and the stage 'production' could not be downloaded from 'https://nitro.test/' after 1 attempts (Nitro returned the status code 503.). Falling back to the fusion configuration that was downloaded at 2026-07-29 12:00:00Z, which may be out of date.
            The source schema 'orders' does not specify a 'devUrl' for its HTTP transport. The composed configuration uses its 'url', which might not be reachable from the local development environment.
            gateway composed against a fusion configuration that could NOT be refreshed. The configuration of the Nitro api QXBpCmdhdGV3YXk for the stage production was downloaded at 2026-07-29 12:00:00Z and may be out of date, run 'nitro login' when the sign-in expired. Source schemas from Nitro: orders (https://orders.example.com/graphql), reviews (https://reviews.dev.example.com/graphql).
            """);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_FailTheGateway_When_NeitherNitroNorTheCacheHasAConfiguration()
    {
        // arrange
        _server.DownloadHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status503ServiceUnavailable);
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(GatewayApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            () => harness.Composition.ComposeOnGatewayStartAsync(
                gateway,
                model,
                compositionGate,
                TestContext.Current.CancellationToken));

        // assert
        $"""
        Exception: {Normalize(exception.Message)}
        StopApplication calls: {harness.Lifetime.StopApplicationCalls}
        Gate count: {compositionGate.CurrentCount}
        Archive written: {File.Exists(_gatewayArchivePath)}
        """.MatchInlineSnapshot(
            """
            Exception: The GraphQL schema composition for 'gateway' failed: The fusion configuration for the api 'QXBpCmdhdGV3YXk' and the stage 'production' could not be downloaded from 'https://nitro.test/' after 1 attempts (Nitro returned the status code 503.).
            StopApplication calls: 0
            Gate count: 1
            Archive written: False
            """);
    }

    [Fact]
    public async Task RunGuardedRecompositionAsync_Should_ReuseTheConfigurationOfTheRun_When_ASourceRestarts()
    {
        // arrange
        await ServeSeedAsync();
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(GatewayApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);
        await harness.Composition.ComposeOnGatewayStartAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // act
        await harness.Composition.RunGuardedRecompositionAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // assert
        $"""
        Downloads: {GetDownloadCount()}
        Source schemas: {await ReadSourceSchemaNamesAsync()}
        """.MatchInlineSnapshot(
            """
            Downloads: 1
            Source schemas: orders, products, reviews
            """);
    }

    [Fact]
    public async Task RunGuardedRecompositionAsync_Should_DoNothing_When_TheGatewayHasNoConfigurationOfTheRun()
    {
        // arrange
        // the start gate failed, so no fusion configuration was acquired for the gateway.
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(GatewayApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        await harness.Composition.RunGuardedRecompositionAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // assert
        $"""
        Requests: {_server.Requests.Count}
        Archive written: {File.Exists(_gatewayArchivePath)}
        Information:
        {DescribeEntries(harness, LogLevel.Information)}
        """.MatchInlineSnapshot(
            """
            Requests: 0
            Archive written: False
            Information:
            Skipping the schema recomposition for gateway because no fusion configuration was acquired for it in this run.
            """);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_ComposeFromTheCache_When_TheSessionExpired()
    {
        // arrange
        await PrimeTheCacheAsync();
        WriteExpiredSession();
        var harness = CompositionHarness.Create(CreateCoordinator(new TestNitroEnvironment()));
        var (model, gateway) = CreateModel(GatewayApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);
        var requestsBeforeTheStartGate = _server.Requests.Count;

        // act
        await harness.Composition.ComposeOnGatewayStartAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // assert
        $"""
        Requests during the start gate: {_server.Requests.Count - requestsBeforeTheStartGate}
        Source schemas: {await ReadSourceSchemaNamesAsync()}
        Fallback warning: {Normalize(GetFallbackWarning(harness))}
        """.MatchInlineSnapshot(
            """
            Requests during the start gate: 0
            Source schemas: orders, products, reviews
            Fallback warning: A fresh fusion configuration could NOT be fetched from Nitro. The Nitro session stored at '<temp>/session.json' expired at 2026-07-29 11:00:00Z. Run 'nitro login' to sign in again. Falling back to the fusion configuration that was downloaded at 2026-07-29 12:00:00Z, which may be out of date.
            """);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_FailTheGateway_When_TheSessionExpiredWithoutACache()
    {
        // arrange
        WriteExpiredSession();
        var harness = CompositionHarness.Create(CreateCoordinator(new TestNitroEnvironment()));
        var (model, gateway) = CreateModel(GatewayApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            () => harness.Composition.ComposeOnGatewayStartAsync(
                gateway,
                model,
                compositionGate,
                TestContext.Current.CancellationToken));

        // assert
        $"""
        Exception: {Normalize(exception.Message)}
        Requests: {_server.Requests.Count}
        Errors:
        {Normalize(DescribeEntries(harness, LogLevel.Error))}
        """.MatchInlineSnapshot(
            """
            Exception: The GraphQL schema composition for 'gateway' failed: The Nitro session stored at '<temp>/session.json' expired at 2026-07-29 11:00:00Z. Run 'nitro login' to sign in again.
            Requests: 0
            Errors:
            The GraphQL schema composition for 'gateway' failed: The Nitro session stored at '<temp>/session.json' expired at 2026-07-29 11:00:00Z. Run 'nitro login' to sign in again.
            """);
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_UseTheApiOfTheGateway_When_ASubgraphSelectsAnApiAsWell()
    {
        // arrange
        await ServeSeedAsync();
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(GatewayApiId, SubgraphApiId);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        await harness.Composition.ComposeOnGatewayStartAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);

        // assert
        var request = Assert.Single(_server.Requests);

        Assert.Equal(
            $"/api/v1/apis/{GatewayApiId}/fusion/configurations/latest/download",
            request.Path);
    }

    [Fact]
    public void ReportNitroConfigurationDiagnostics_Should_Warn_When_AnApiIdCannotTakeEffect()
    {
        // arrange
        var harness = CompositionHarness.Create(coordinator: null);
        var (model, gateway) = CreateModel(GatewayApiId, SubgraphApiId);

        // act
        harness.Composition.ReportNitroConfigurationDiagnostics(model, [gateway]);

        // assert
        DescribeEntries(harness, LogLevel.Warning).MatchInlineSnapshot(
            """
            The resource products selects the Nitro api QXBpCnByb2R1Y3Rz, but the distributed application does not add Nitro. Call AddNitro on the distributed application builder so the api id takes effect.
            The resource gateway selects the Nitro api QXBpCmdhdGV3YXk, but the distributed application does not add Nitro. Call AddNitro on the distributed application builder so the api id takes effect.
            """);
    }

    [Fact]
    public void ReportNitroConfigurationDiagnostics_Should_Warn_When_NoGatewaySelectsAnApi()
    {
        // arrange
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(gatewayApiId: null);

        // act
        harness.Composition.ReportNitroConfigurationDiagnostics(model, [gateway]);

        // assert
        DescribeEntries(harness, LogLevel.Warning).MatchInlineSnapshot(
            "Nitro is added for the stage production, but no composed schema selects a Nitro api. "
            + "Call WithNitroApiId on the gateway that composes against the fusion configuration "
            + "of Nitro.");
    }

    [Fact]
    public void ReportNitroConfigurationDiagnostics_Should_StaySilent_When_TheGatewaySelectsAnApi()
    {
        // arrange
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (model, gateway) = CreateModel(GatewayApiId);

        // act
        harness.Composition.ReportNitroConfigurationDiagnostics(model, [gateway]);

        // assert
        Assert.Equal(string.Empty, DescribeEntries(harness, LogLevel.Warning));
    }

    [Fact]
    public async Task AddNitroPortalUrlsAsync_Should_AttachDerivedPortal_When_GatewayUsesNitro()
    {
        // arrange
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (_, gateway) = CreateModel(GatewayApiId);

        // act
        await harness.Composition.AddNitroPortalUrlsAsync(
            [gateway],
            TestContext.Current.CancellationToken);

        // assert
        var annotation = Assert.Single(
            gateway.Annotations.OfType<ResourceUrlAnnotation>(),
            url => url.DisplayText == "Nitro Portal");
        $"""
        Target: {annotation.Url}
        Display: {annotation.DisplayText}
        Location: {annotation.DisplayLocation}
        """
            .Replace(_server.BaseAddress.OriginalString, "<api>", StringComparison.Ordinal)
            .MatchInlineSnapshot(
            """
            Target: <api>/ui
            Display: Nitro Portal
            Location: SummaryAndDetails
            """);
    }

    [Fact]
    public async Task AddNitroPortalUrlsAsync_Should_UseOverrideVerbatim_When_PortalIsConfigured()
    {
        // arrange
        var portalUrl = new Uri("https://portal.example.test/custom?tenant=abc");
        var harness = CompositionHarness.Create(CreateCoordinator(), portalUrl);
        var (_, gateway) = CreateModel(GatewayApiId);

        // act
        await harness.Composition.AddNitroPortalUrlsAsync(
            [gateway],
            TestContext.Current.CancellationToken);

        // assert
        var annotation = Assert.Single(
            gateway.Annotations.OfType<ResourceUrlAnnotation>(),
            url => url.DisplayText == "Nitro Portal");
        Assert.Equal(portalUrl.OriginalString, annotation.Url);
    }

    [Fact]
    public async Task AddNitroPortalUrlsAsync_Should_NotAttachPortal_When_GatewayIsLocal()
    {
        // arrange
        var harness = CompositionHarness.Create(CreateCoordinator());
        var (_, gateway) = CreateModel(gatewayApiId: null);

        // act
        await harness.Composition.AddNitroPortalUrlsAsync(
            [gateway],
            TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(gateway.Annotations.OfType<ResourceUrlAnnotation>());
    }

    [Fact]
    public async Task ComposeOnGatewayStartAsync_Should_NotWaitOrFail_When_ValidationIsUnavailable()
    {
        // arrange
        await ServeSeedAsync();
        var validator = new RecordingSchemaValidator(blockUntilReleased: true);
        var harness = CompositionHarness.Create(
            CreateCoordinator(validator),
            notifier: new NoopValidationNotifier());
        var (model, gateway) = CreateModel(GatewayApiId, enableValidation: true);
        using var compositionGate = new SemaphoreSlim(1, 1);

        // act
        await harness.Composition.ComposeOnGatewayStartAsync(
                gateway,
                model,
                compositionGate,
                TestContext.Current.CancellationToken)
            .WaitAsync(
                TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken);
        await validator.Started.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken);
        var validationWasStillBlocked = validator.IsBlocked;
        validator.Release();
        await WaitForValidationReportAsync(
            harness.ValidationCoordinator,
            gateway.Name,
            report => report.Status is NitroSchemaValidationStatus.Unavailable);
        harness.Lifetime.StopApplication();

        // assert
        $"""
        Archive installed: {File.Exists(_gatewayArchivePath)}
        Composition gate count: {compositionGate.CurrentCount}
        Validation calls: {validator.SchemaHashes.Count}
        Validation blocked after composition: {validationWasStillBlocked}
        Validation status: {harness.ValidationCoordinator.GetLatestReport(gateway.Name)?.Status}
        """.MatchInlineSnapshot(
            """
            Archive installed: True
            Composition gate count: 1
            Validation calls: 1
            Validation blocked after composition: True
            Validation status: Unavailable
            """);
    }

    [Fact]
    public async Task ExecuteRecomposeCommandAsync_Should_ComposeOnceAndValidateOnlyChangedSchema()
    {
        // arrange
        await ServeSeedAsync();
        var validator = new RecordingSchemaValidator(blockUntilReleased: false);
        var coordinator = CreateCoordinator(validator);
        var harness = CompositionHarness.Create(
            coordinator,
            notifier: new NoopValidationNotifier());
        var (model, gateway) = CreateModel(GatewayApiId, enableValidation: true);
        using var compositionGate = new SemaphoreSlim(1, 1);
        await coordinator.AcquireSeedAsync(
            gateway.Name,
            GatewayApiId,
            new RecordingLogger<NitroSchemaCompositionTests>(),
            TestContext.Current.CancellationToken);

        // act
        var first = await harness.Composition.ExecuteRecomposeCommandAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);
        var firstReport = await WaitForValidationReportAsync(
            harness.ValidationCoordinator,
            gateway.Name,
            _ => true);
        var compositionsAfterFirst = CountRecompositions(harness);

        var unchanged = await harness.Composition.ExecuteRecomposeCommandAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);
        var validationsAfterUnchanged = validator.SchemaHashes.Count;

        await File.WriteAllTextAsync(
            IOPath.Combine(
                IOPath.GetDirectoryName(_productsProjectFile)!,
                "schema.graphqls"),
            "type Query { product: String @deprecated }",
            TestContext.Current.CancellationToken);
        var changed = await harness.Composition.ExecuteRecomposeCommandAsync(
            gateway,
            model,
            compositionGate,
            TestContext.Current.CancellationToken);
        await WaitForValidationReportAsync(
            harness.ValidationCoordinator,
            gateway.Name,
            report => !string.Equals(
                report.SchemaHash,
                firstReport.SchemaHash,
                StringComparison.Ordinal));
        harness.Lifetime.StopApplication();

        // assert
        $"""
        First: {first.Success} ({first.Message})
        Unchanged: {unchanged.Success} ({unchanged.Message})
        Changed: {changed.Success} ({changed.Message})
        Compositions after first command: {compositionsAfterFirst}
        Validations after unchanged command: {validationsAfterUnchanged}
        Validations after changed command: {validator.SchemaHashes.Count}
        Gate count: {compositionGate.CurrentCount}
        """.MatchInlineSnapshot(
            """
            First: True (Schema composition completed)
            Unchanged: True (Schema composition completed)
            Changed: True (Schema composition completed)
            Compositions after first command: 1
            Validations after unchanged command: 1
            Validations after changed command: 2
            Gate count: 1
            """);
    }

    private (DistributedApplicationModel Model, IResourceWithEndpoints Gateway) CreateModel(
        string? gatewayApiId,
        string? productsApiId = null,
        bool enableValidation = false)
    {
        var builder = DistributedApplication.CreateBuilder();
        var products = builder
            .AddProject("products", _productsProjectFile)
            .WithGraphQLSchemaFile();

        if (productsApiId is not null)
        {
            products.WithNitroApiId(productsApiId);
        }

        var gateway = builder
            .AddProject("gateway", _gatewayProjectFile)
            .WithGraphQLSchemaComposition()
            .WithReference(products);

        if (gatewayApiId is not null)
        {
            gateway.WithNitroApiId(gatewayApiId);
        }

        if (enableValidation)
        {
            gateway.Resource.Annotations.Add(new NitroSchemaValidationAnnotation());
        }

        var model = new DistributedApplicationModel(builder.Resources);

        return (model, model.GetGraphQLCompositionResources().Single());
    }

    private NitroSeedCoordinator CreateCoordinator()
        => CreateCoordinator(
            CreateDefaultEnvironment());

    private NitroSeedCoordinator CreateCoordinator(INitroSchemaValidator validator)
        => CreateCoordinator(CreateDefaultEnvironment(), validator);

    private NitroSeedCoordinator CreateCoordinator(
        INitroEnvironment environment,
        INitroSchemaValidator? validator = null)
        => new(
            Stage,
            new NitroConnectionResolver(
                new NitroSessionReader(_directory.GetPath("session.json"), TimeSpan.Zero),
                environment,
                NitroDefaults.ApiUrl,
                _timeProvider,
                NitroDefaults.AccessTokenExpiryGrace),
            new NitroSeedProvider(
                new NitroFusionConfigurationDownloader(
                    _httpClient,
                    new NitroDownloadRetryPolicy(
                        attemptsWithCachedSeed: 1,
                        attemptsWithoutCachedSeed: 1,
                        TimeSpan.Zero),
                    TimeProvider.System),
                new NitroSeedCache(_directory.GetPath("cache"), _timeProvider),
                new NitroApiLookupClient(
                    GraphQLHttpClient.Create(_httpClient, disposeHttpClient: false))),
            validator
                ?? new NitroSchemaValidator(
                    GraphQLHttpClient.Create(_httpClient, disposeHttpClient: false),
                    _timeProvider,
                    new RecordingLogger<NitroSchemaValidator>()),
            _directory.GetPath("run"));

    private TestNitroEnvironment CreateDefaultEnvironment()
        => new(
            (NitroEnvironmentVariables.CloudUrl, _server.BaseAddress.AbsoluteUri),
            (NitroEnvironmentVariables.ApiKey, "nitro-api-key"));

    private static int CountRecompositions(CompositionHarness harness)
        => harness.Logger.Entries.Count(
            entry => entry.Level is LogLevel.Information
                && entry.Message.StartsWith(
                    "Recomposing GraphQL schema for ",
                    StringComparison.Ordinal));

    private static async Task<NitroSchemaValidationReport> WaitForValidationReportAsync(
        NitroSchemaValidationCoordinator coordinator,
        string resourceName,
        Func<NitroSchemaValidationReport, bool> predicate)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

        while (coordinator.GetLatestReport(resourceName) is not { } report
            || !predicate(report))
        {
            await Task.Delay(10, timeout.Token);
        }

        return coordinator.GetLatestReport(resourceName)!;
    }

    /// <summary>
    /// Serves the fusion configuration of Nitro: an outdated products schema that the local one
    /// replaces, a schema with a development URL and a schema without one.
    /// </summary>
    private async Task ServeSeedAsync()
    {
        var archive = await NitroTestArchive.CreateAsync(
            TestContext.Current.CancellationToken,
            new NitroTestSourceSchema(
                "products",
                "type Query { staleProduct: String }",
                CreateSettings("products", "https://stale.example.com/graphql")),
            new NitroTestSourceSchema(
                "orders",
                "type Query { order: String }",
                """
                {
                  "name": "orders",
                  "transports": {
                    "http": {
                      "url": "{{ORDERS_URL}}"
                    }
                  },
                  "environments": {
                    "production": {
                      "ORDERS_URL": "https://orders.example.com/graphql"
                    }
                  }
                }
                """),
            new NitroTestSourceSchema(
                "reviews",
                "type Query { review: String }",
                """
                {
                  "name": "reviews",
                  "transports": {
                    "http": {
                      "url": "{{REVIEWS_URL}}",
                      "devUrl": "{{REVIEWS_DEV_URL}}"
                    }
                  },
                  "environments": {
                    "Aspire": {
                      "REVIEWS_URL": "https://reviews.aspire.example.com/graphql",
                      "REVIEWS_DEV_URL": "https://reviews.aspire.example.com/graphql"
                    },
                    "production": {
                      "REVIEWS_URL": "https://reviews.example.com/graphql",
                      "REVIEWS_DEV_URL": "https://reviews.dev.example.com/graphql"
                    }
                  }
                }
                """));

        _server.DownloadHandler = _ => FakeNitroResponse.Archive(archive);
    }

    private async Task PrimeTheCacheAsync()
    {
        await ServeSeedAsync();

        var acquisition = await CreateCoordinator().AcquireSeedAsync(
            "gateway",
            GatewayApiId,
            new RecordingLogger<NitroSchemaCompositionTests>(),
            TestContext.Current.CancellationToken);

        Assert.NotNull(acquisition.Seed);
    }

    private void WriteExpiredSession()
    {
        var header = Base64UrlEncode("""{"alg":"RS256","typ":"JWT"}""");
        var payload = Base64UrlEncode(
            JsonSerializer.Serialize(
                new Dictionary<string, string>
                {
                    ["api_url"] = _server.BaseAddress.AbsoluteUri
                }));
        var accessToken = $"{header}.{payload}.signature";

        _directory.WriteFile(
            "session.json",
            $$"""
            {
              "email": "dev@example.com",
              "tokens": {
                "accessToken": "{{accessToken}}",
                "expiresAt": "2026-07-29T11:00:00+00:00"
              },
              "workspace": {
                "id": "V29ya3NwYWNlCmRlbW8",
                "name": "demo"
              }
            }
            """);
    }

    private static string Base64UrlEncode(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string CreateSettings(string name, string url)
        => $$"""
        {
          "name": "{{name}}",
          "transports": {
            "http": {
              "url": "{{url}}"
            }
          }
        }
        """;

    private async Task<string> DescribeArchiveAsync(CancellationToken cancellationToken)
    {
        using var archive = FusionArchive.Open(_gatewayArchivePath);
        var sourceSchemaNames = await archive.GetSourceSchemaNamesAsync(cancellationToken);
        using var products = await archive.TryGetSourceSchemaConfigurationAsync(
            "products",
            cancellationToken);
        using var gatewayConfiguration = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            cancellationToken);
        var settings = JsonSerializer.Serialize(
            gatewayConfiguration!.Settings.RootElement,
            new JsonSerializerOptions { WriteIndented = true });

        return $"""
            Source schemas: {string.Join(", ", sourceSchemaNames)}
            products document: {await ReadSchemaAsync(products!, cancellationToken)}
            Gateway settings:
            {settings}
            """;
    }

    private async Task<string> ReadSourceSchemaNamesAsync()
        => string.Join(
            ", ",
            await NitroTestArchive.ReadSourceSchemaNamesAsync(
                _gatewayArchivePath,
                TestContext.Current.CancellationToken));

    private static async Task<string> ReadSchemaAsync(
        SourceSchemaConfiguration configuration,
        CancellationToken cancellationToken)
    {
        await using var stream = await configuration.OpenReadSchemaAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        return (await reader.ReadToEndAsync(cancellationToken)).Trim();
    }

    private int GetDownloadCount()
        => _server.Requests.Count(request =>
            request.Path.StartsWith("/api/v1/apis/", StringComparison.Ordinal));

    private string DescribeWarnings(CompositionHarness harness)
        => Normalize(DescribeEntries(harness, LogLevel.Warning));

    private static string DescribeEntries(CompositionHarness harness, LogLevel level)
        => string.Join(
            Environment.NewLine,
            harness.Logger.Entries
                .Where(entry => entry.Level == level)
                .Select(entry => entry.Message));

    private static string GetFallbackWarning(CompositionHarness harness)
        => Assert.Single(
                harness.Logger.Entries,
                entry => entry.Message.StartsWith(
                    "A fresh fusion configuration could NOT be fetched",
                    StringComparison.Ordinal))
            .Message;

    private string Normalize(string value)
        => value
            .Replace(_server.BaseAddress.AbsoluteUri, "https://nitro.test/", StringComparison.Ordinal)
            .Replace(_directory.Path, "<temp>", StringComparison.Ordinal);

    private sealed class RecordingSchemaValidator(bool blockUntilReleased)
        : INitroSchemaValidator
    {
        private readonly TaskCompletionSource _started =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ConcurrentQueue<string> SchemaHashes { get; } = [];

        public Task Started => _started.Task;

        public bool IsBlocked => blockUntilReleased && !_release.Task.IsCompleted;

        public void Release() => _release.TrySetResult();

        public async Task<NitroSchemaValidationReport> ValidateAsync(
            NitroConnection connection,
            string apiId,
            string stage,
            byte[] schema,
            string schemaHash,
            CancellationToken cancellationToken)
        {
            SchemaHashes.Enqueue(schemaHash);
            _started.TrySetResult();

            if (blockUntilReleased)
            {
                await _release.Task.WaitAsync(cancellationToken);
                return NitroSchemaValidationReport.Unavailable(
                    schemaHash,
                    "Nitro is unavailable.",
                    DateTimeOffset.UtcNow,
                    "request-unavailable");
            }

            return NitroSchemaValidationReport.Passed(
                schemaHash,
                $"request-{SchemaHashes.Count}",
                DateTimeOffset.UtcNow);
        }
    }

    private sealed class NoopValidationNotifier : INitroSchemaValidationNotifier
    {
        public void NotifyViolations(string message)
        {
        }

        public void NotifyRestored(string message)
        {
        }
    }
}
