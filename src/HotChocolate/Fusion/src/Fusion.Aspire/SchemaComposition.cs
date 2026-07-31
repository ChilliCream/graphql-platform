using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using HotChocolate.Fusion.Aspire.Nitro;
using HotChocolate.Fusion.Packaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

internal sealed class SchemaComposition(
    ResourceNotificationService resourceNotificationService,
    ResourceLoggerService resourceLoggerService,
    IHostApplicationLifetime lifetime,
    NitroCompositionOptions nitroOptions,
    NitroSchemaValidationCoordinator validationCoordinator,
    GatewayCompositionCommandCoordinator commandCoordinator,
    ILogger<SchemaComposition> logger)
    : IDistributedApplicationEventingSubscriber
{
    private const int FetchMaxRetries = 15;
    private const int ArchiveCopyMaxAttempts = 5;
    private const string NitroPortalDisplayText = "🌐 Nitro";
    private static readonly TimeSpan s_fetchRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan s_recompositionDebounceDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_archiveCopyRetryDelay = TimeSpan.FromMilliseconds(250);

    public Task SubscribeAsync(
        IDistributedApplicationEventing eventing,
        DistributedApplicationExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        if (!executionContext.IsRunMode)
        {
            return Task.CompletedTask;
        }

        if (nitroOptions.Coordinator is { } seedCoordinator)
        {
            lifetime.ApplicationStopping.Register(seedCoordinator.DeleteRunSeeds);
        }

        // All subscriptions must exist before any resource starts. A fast resource can publish
        // its initial ResourceReadyEvent early, and events are not replayed to late subscribers,
        // so subscribing any later would misclassify the first restart of such a resource as its
        // initial start.
        eventing.Subscribe<BeforeStartEvent>(async (@event, beforeStartCancellationToken) =>
        {
            var compositionResources = @event.Model.GetGraphQLCompositionResources().ToList();

            ReportNitroConfigurationDiagnostics(@event.Model, compositionResources);
            await AddNitroPortalUrlsAsync(
                compositionResources,
                beforeStartCancellationToken);

            if (compositionResources.Count == 0)
            {
                logger.LogDebug("No resources found that need GraphQL schema composition");
                return;
            }

            // One gate per gateway serializes the startup composition with recompositions
            // triggered by source schema restarts. Different gateways stay independent and
            // may compose concurrently.
            var compositionGates = compositionResources.ToDictionary(
                gateway => gateway,
                _ => new SemaphoreSlim(1, 1));

            var recompositionWorkers = SubscribeToSourceSchemaRestarts(
                eventing,
                @event.Model,
                compositionResources,
                compositionGates);

            foreach (var compositionResource in compositionResources)
            {
                var gateway = compositionResource;
                var compositionGate = compositionGates[gateway];

                commandCoordinator.Register(
                    gateway.Name,
                    commandCancellationToken => ExecuteRecomposeCommandAsync(
                        gateway,
                        @event.Model,
                        compositionGate,
                        commandCancellationToken));

                // The orchestrator awaits this handler before it starts the gateway process,
                // so the gateway only starts once its fusion archive is on disk. The handler
                // runs again on every restart of the gateway, which composes a fresh schema.
                eventing.Subscribe<BeforeResourceStartedEvent>(
                    gateway,
                    (_, startCancellationToken) => ComposeOnGatewayStartAsync(
                        gateway,
                        @event.Model,
                        compositionGate,
                        startCancellationToken));
            }

            // Restart triggers only arrive after the initial ResourceReadyEvent of a source,
            // so the workers can start right away. The composition gates guarantee that a
            // recomposition never overlaps the startup composition of the same gateway.
            StartRecompositionWorkers(recompositionWorkers);

            logger.LogInformation("Watching source schema resources for restarts.");
        });

        return Task.CompletedTask;
    }

    internal async Task AddNitroPortalUrlsAsync(
        IReadOnlyList<IResourceWithEndpoints> compositionResources,
        CancellationToken cancellationToken)
    {
        var coordinator = nitroOptions.Coordinator;
        if (coordinator is null
            || !compositionResources.Any(resource => resource.GetNitroApiId() is not null))
        {
            return;
        }

        string portalUrl;
        if (nitroOptions.PortalUrl is { } configured)
        {
            portalUrl = configured.OriginalString;
        }
        else
        {
            try
            {
                var connection = await coordinator.ResolveConnectionAsync(logger, cancellationToken);
                portalUrl = NitroDefaults.CreatePortalUrl(connection.ApiUrl).OriginalString;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                logger.LogWarning(
                    "The Nitro Portal URL could not be resolved. The informational link will "
                    + "not be added.");
                return;
            }
        }

        foreach (var resource in compositionResources)
        {
            if (resource.GetNitroApiId() is null
                || resource.Annotations.OfType<ResourceUrlAnnotation>()
                    .Any(url => url.DisplayText == NitroPortalDisplayText))
            {
                continue;
            }

            resource.Annotations.Add(
                new ResourceUrlAnnotation
                {
                    Url = portalUrl,
                    DisplayText = NitroPortalDisplayText,
                    DisplayLocation = UrlDisplayLocation.SummaryAndDetails
                });
        }
    }

    /// <summary>
    /// Reports the configurations that cannot take effect: an api id without Nitro, and Nitro
    /// without a gateway that selects a fusion configuration.
    /// </summary>
    internal void ReportNitroConfigurationDiagnostics(
        DistributedApplicationModel appModel,
        IReadOnlyList<IResourceWithEndpoints> compositionResources)
    {
        var coordinator = nitroOptions.Coordinator;

        if (coordinator is null)
        {
            foreach (var resource in appModel.Resources)
            {
                if (resource.GetNitroApiId() is not { } apiId)
                {
                    continue;
                }

                CreateGatewayLogger(resource).LogWarning(
                    "The resource {ResourceName} selects the Nitro api {ApiId}, but the "
                    + "distributed application does not add Nitro. Call AddNitro on the "
                    + "distributed application builder so the api id takes effect.",
                    resource.Name,
                    apiId);
            }

            return;
        }

        if (compositionResources.Any(gateway => gateway.GetNitroApiId() is not null))
        {
            return;
        }

        logger.LogWarning(
            "Nitro is added for the stage {Stage}, but no composed schema selects a Nitro api. "
            + "Call WithNitroApiId on the gateway that composes against the fusion configuration "
            + "of Nitro.",
            coordinator.Stage);
    }

    private void StartRecompositionWorkers(List<GatewayRecompositionWorker> recompositionWorkers)
    {
        foreach (var worker in recompositionWorkers)
        {
            _ = worker.RunAsync(lifetime.ApplicationStopping);
        }
    }

    /// <summary>
    /// Composes the schema for a gateway before the orchestrator starts the gateway process.
    /// A failed composition marks the gateway as failed to start without stopping the rest
    /// of the application.
    /// </summary>
    internal async Task ComposeOnGatewayStartAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        SemaphoreSlim compositionGate,
        CancellationToken cancellationToken)
    {
        var outcome = SchemaCompositionOutcome.Failed;
        await compositionGate.WaitAsync(cancellationToken);

        try
        {
            logger.LogInformation(
                "Composing the GraphQL schema for {ResourceName} before it starts.",
                compositionResource.Name);

            Exception? failure = null;

            try
            {
                var seed = await PrepareCompositionAsync(
                    compositionResource,
                    appModel,
                    cancellationToken);

                outcome = await ComposeSchemaAsync(
                    compositionResource,
                    appModel,
                    seed,
                    cancellationToken);
                if (outcome.Success)
                {
                    failure = null;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failure = exception;
            }

            if (!outcome.Success)
            {
                var message = failure is null
                    ? $"The GraphQL schema composition for '{compositionResource.Name}' failed."
                    : $"The GraphQL schema composition for '{compositionResource.Name}' failed: {failure.Message}";

                logger.LogError(failure, "{Message}", message);
                resourceLoggerService.GetLogger(compositionResource).LogError(failure, "{Message}", message);

                throw failure is null
                    ? new DistributedApplicationException(message)
                    : new DistributedApplicationException(message, failure);
            }
        }
        finally
        {
            compositionGate.Release();
        }

        if (outcome.GatewaySchema is not null)
        {
            validationCoordinator.Schedule(compositionResource, outcome.GatewaySchema);
        }
    }

    /// <summary>
    /// Waits until the source schema resources of the gateway are ready and, for a gateway that
    /// selects a Nitro api, acquires the fusion configuration it composes against. The wait and
    /// the download run at the same time, so their delays do not add up.
    /// </summary>
    /// <returns>
    /// The fusion configuration the gateway composes against, or <c>null</c> when the gateway
    /// composes the source schemas of the distributed application alone.
    /// </returns>
    private async Task<NitroGatewaySeed?> PrepareCompositionAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken)
    {
        var coordinator = nitroOptions.Coordinator;
        var apiId = compositionResource.GetNitroApiId();

        if (coordinator is null || apiId is null)
        {
            await WaitForSourceSchemaResourcesReadyAsync(
                compositionResource,
                appModel,
                cancellationToken);

            return null;
        }

        using var seedCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var seedTask = AcquireSeedAsync(
            coordinator,
            compositionResource,
            apiId,
            seedCancellation.Token);

        try
        {
            await WaitForSourceSchemaResourcesReadyAsync(
                compositionResource,
                appModel,
                cancellationToken);
        }
        catch
        {
            // The gateway will not start, so its fusion configuration is not needed anymore.
            // The acquisition reports every failure as a result, so awaiting it here cannot
            // replace the failure of the wait.
            await seedCancellation.CancelAsync();
            await seedTask;

            throw;
        }

        var acquisition = await seedTask;

        cancellationToken.ThrowIfCancellationRequested();

        if (acquisition.FailureMessage is { } failureMessage)
        {
            throw new DistributedApplicationException(failureMessage);
        }

        return acquisition.Seed;
    }

    /// <summary>
    /// Acquires the fusion configuration of a gateway. Every failure, including one that the file
    /// system raises, is reported as a result so that it fails this gateway alone.
    /// </summary>
    private async Task<NitroSeedAcquisition> AcquireSeedAsync(
        NitroSeedCoordinator coordinator,
        IResourceWithEndpoints compositionResource,
        string apiId,
        CancellationToken cancellationToken)
    {
        var gatewayLogger = CreateGatewayLogger(compositionResource);

        try
        {
            return await coordinator.AcquireSeedAsync(
                compositionResource.Name,
                apiId,
                gatewayLogger,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogDebug(
                exception,
                "The fusion configuration of the Nitro api {ApiId} could not be acquired for "
                + "{ResourceName}.",
                apiId,
                compositionResource.Name);

            return NitroSeedAcquisition.Failed(
                $"The fusion configuration of the Nitro api '{apiId}' for the stage "
                + $"'{coordinator.Stage}' could not be acquired: {exception.Message}");
        }
    }

    /// <summary>
    /// Waits until every endpoint-based source schema resource of the gateway is healthy.
    /// File-based source schemas need no wait. A source that becomes unavailable fails the
    /// wait with an error that names the source.
    /// </summary>
    internal async Task WaitForSourceSchemaResourcesReadyAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken)
    {
        foreach (var referencedResource in GetReferencedResources(compositionResource, appModel))
        {
            var annotation = referencedResource.Annotations
                .OfType<GraphQLSourceSchemaAnnotation>()
                .FirstOrDefault();

            if (annotation is not { Location: SourceSchemaLocationType.SchemaEndpoint })
            {
                continue;
            }

            logger.LogDebug(
                "Waiting for source schema resource {ResourceName} to become healthy",
                referencedResource.Name);

            try
            {
                await resourceNotificationService.WaitForResourceHealthyAsync(
                    referencedResource.Name,
                    WaitBehavior.StopOnResourceUnavailable,
                    cancellationToken);
            }
            catch (DistributedApplicationException exception)
            {
                throw new DistributedApplicationException(
                    $"The source schema resource '{referencedResource.Name}' required by "
                    + $"'{compositionResource.Name}' did not become healthy.",
                    exception);
            }
        }
    }

    private List<GatewayRecompositionWorker> SubscribeToSourceSchemaRestarts(
        IDistributedApplicationEventing eventing,
        DistributedApplicationModel appModel,
        List<IResourceWithEndpoints> compositionResources,
        Dictionary<IResourceWithEndpoints, SemaphoreSlim> compositionGates)
    {
        var sourceToGateways = BuildSourceToGatewayMap(compositionResources, appModel);
        var restartTracker = new SourceSchemaRestartTracker();
        var workerByGateway = new Dictionary<IResourceWithEndpoints, GatewayRecompositionWorker>();

        foreach (var (sourceResource, gateways) in sourceToGateways)
        {
            var affectedWorkers = new List<GatewayRecompositionWorker>();

            foreach (var gateway in gateways)
            {
                if (!workerByGateway.TryGetValue(gateway, out var worker))
                {
                    var gatewayResource = gateway;
                    var compositionGate = compositionGates[gateway];
                    worker = new GatewayRecompositionWorker(
                        gateway.Name,
                        composeCt => RunGuardedRecompositionAsync(
                            gatewayResource,
                            appModel,
                            compositionGate,
                            composeCt),
                        s_recompositionDebounceDelay,
                        TimeProvider.System,
                        logger);
                    workerByGateway.Add(gateway, worker);
                }

                affectedWorkers.Add(worker);
            }

            eventing.Subscribe<ResourceReadyEvent>(
                sourceResource,
                (resourceReady, _) =>
                {
                    if (restartTracker.IsRestart(resourceReady.Resource.Name))
                    {
                        logger.LogInformation(
                            "Source schema resource {ResourceName} restarted. Scheduling schema recomposition.",
                            resourceReady.Resource.Name);

                        foreach (var worker in affectedWorkers)
                        {
                            worker.TriggerRecomposition();
                        }
                    }

                    return Task.CompletedTask;
                });
        }

        return [.. workerByGateway.Values];
    }

    /// <summary>
    /// Runs a schema recomposition while holding the composition gate of the gateway so that
    /// it never overlaps a startup composition for the same gateway.
    /// </summary>
    internal async Task RunGuardedRecompositionAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        SemaphoreSlim compositionGate,
        CancellationToken cancellationToken)
    {
        SchemaCompositionOutcome outcome;
        await compositionGate.WaitAsync(cancellationToken);

        try
        {
            outcome = await RecomposeSchemaAsync(
                compositionResource,
                appModel,
                cancellationToken);
        }
        finally
        {
            compositionGate.Release();
        }

        if (outcome.GatewaySchema is not null)
        {
            validationCoordinator.Schedule(compositionResource, outcome.GatewaySchema);
        }
    }

    internal async Task<ExecuteCommandResult> ExecuteRecomposeCommandAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        SemaphoreSlim compositionGate,
        CancellationToken cancellationToken)
    {
        bool enteredCompositionGate;
        try
        {
            enteredCompositionGate = await compositionGate.WaitAsync(0, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CommandResults.Canceled();
        }

        if (!enteredCompositionGate)
        {
            return CommandResults.Success("Composition already in progress");
        }

        SchemaCompositionOutcome outcome;
        try
        {
            outcome = await RecomposeSchemaAsync(
                compositionResource,
                appModel,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return CommandResults.Canceled();
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Manual schema composition failed for {ResourceName}.",
                compositionResource.Name);
            return CommandResults.Failure(
                $"Schema composition failed for '{compositionResource.Name}'.");
        }
        finally
        {
            compositionGate.Release();
        }

        if (!outcome.Success)
        {
            return CommandResults.Failure(
                $"Schema composition failed for '{compositionResource.Name}'.");
        }

        if (outcome.GatewaySchema is not null)
        {
            validationCoordinator.Schedule(compositionResource, outcome.GatewaySchema);
        }

        return CommandResults.Success("Schema composition completed");
    }

    /// <summary>
    /// Maps every source schema resource to the gateways whose composed schema depends on it.
    /// </summary>
    internal static Dictionary<IResourceWithEndpoints, List<IResourceWithEndpoints>> BuildSourceToGatewayMap(
        IReadOnlyList<IResourceWithEndpoints> compositionResources,
        DistributedApplicationModel appModel)
    {
        var map = new Dictionary<IResourceWithEndpoints, List<IResourceWithEndpoints>>();

        foreach (var gateway in compositionResources)
        {
            foreach (var referencedResource in GetReferencedResources(gateway, appModel))
            {
                if (!referencedResource.HasGraphQLSchema())
                {
                    continue;
                }

                if (!map.TryGetValue(referencedResource, out var gateways))
                {
                    gateways = [];
                    map.Add(referencedResource, gateways);
                }

                if (!gateways.Contains(gateway))
                {
                    gateways.Add(gateway);
                }
            }
        }

        return map;
    }

    private async Task<SchemaCompositionOutcome> RecomposeSchemaAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken)
    {
        var coordinator = nitroOptions.Coordinator;
        var apiId = compositionResource.GetNitroApiId();
        NitroGatewaySeed? seed = null;

        if (coordinator is not null && apiId is not null)
        {
            // The fusion configuration of a run is fetched once, while the gateway starts, and
            // every recomposition of the run builds on that same configuration.
            seed = coordinator.GetSeed(compositionResource.Name);

            if (seed is null)
            {
                logger.LogInformation(
                    "Skipping the schema recomposition for {ResourceName} because no fusion "
                    + "configuration was acquired for it in this run.",
                    compositionResource.Name);
                return SchemaCompositionOutcome.Failed;
            }
        }

        logger.LogInformation(
            "Recomposing GraphQL schema for {ResourceName}...",
            compositionResource.Name);

        var outcome = await ComposeSchemaAsync(
            compositionResource,
            appModel,
            seed,
            cancellationToken);
        if (outcome.Success)
        {
            logger.LogInformation(
                "Schema recomposition for {ResourceName} completed.",
                compositionResource.Name);
        }
        else
        {
            logger.LogError(
                "Schema recomposition for {ResourceName} failed. The gateway keeps the previous schema.",
                compositionResource.Name);
        }

        return outcome;
    }

    private async Task<SchemaCompositionOutcome> ComposeSchemaAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        NitroGatewaySeed? seed,
        CancellationToken cancellationToken)
    {
        var settings = compositionResource.GetCompositionSettings();

        if (settings is null)
        {
            return SchemaCompositionOutcome.SucceededWithoutSchema;
        }

        // Composition problems are reported to the application host log and to the console
        // log of the gateway resource so that failures are visible on the resource itself.
        var compositionLogger = CreateGatewayLogger(compositionResource);

        logger.LogInformation(
            "Preparing schema composition for {ResourceName}.",
            compositionResource.Name);

        try
        {
            var sourceSchemas = await DiscoverReferencedSourceSchemasAsync(
                compositionResource,
                appModel,
                cancellationToken);

            if (sourceSchemas.Count == 0)
            {
                compositionLogger.LogWarning(
                    "{ResourceName} has no source schemas.",
                    compositionResource.Name);

                // A source schema that cannot be loaded fails the discovery with an
                // exception, so reaching this point means the gateway genuinely
                // references no resources with a source schema annotation. There is
                // nothing to compose, so no archive is written and the composition
                // succeeds with the warning above.
                return SchemaCompositionOutcome.SucceededWithoutSchema;
            }

            try
            {
                var gatewayDirectory = GetProjectPath(compositionResource)!;
                var archivePath = IOPath.Combine(IOPath.GetDirectoryName(gatewayDirectory)!, settings.OutputFileName);
                return await ComposeSchemaAsync(
                    compositionResource.Name,
                    archivePath,
                    seed,
                    sourceSchemas,
                    settings,
                    compositionLogger,
                    compositionResource.HasNitroSchemaValidation(),
                    cancellationToken);
            }
            finally
            {
                foreach (var sourceSchema in sourceSchemas)
                {
                    sourceSchema.SchemaSettings.Dispose();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            compositionLogger.LogError(
                ex,
                "Schema composition failed for {ResourceName}: {Error}",
                compositionResource.Name,
                ex.Message);
        }

        return SchemaCompositionOutcome.Failed;
    }

    internal async Task<List<SourceSchemaInfo>> DiscoverReferencedSourceSchemasAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken)
    {
        var sourceSchemas = new List<SourceSchemaInfo>();

        var referencedResources =
            GraphQLResourceModel.GetReferencedSourceSchemas(compositionResource, appModel);

        logger.LogInformation(
            "Found {Count} referenced resources for {ResourceName}",
            referencedResources.Count, compositionResource.Name);

        try
        {
            foreach (var referencedSource in referencedResources)
            {
                var schemaInfo = await GetSourceSchemaInfoAsync(
                    referencedSource.Resource,
                    referencedSource.Declaration,
                    cancellationToken);
                if (schemaInfo is null)
                {
                    // Composing without this source would either drop it from the composed
                    // schema or silently carry its previous schema forward from the existing
                    // archive, so the composition must fail instead. On startup this marks
                    // the gateway as failed to start, on a recomposition the gateway keeps
                    // the previous schema.
                    throw new InvalidOperationException(
                        $"The source schema for resource '{referencedSource.Resource.Name}' "
                        + "could not be loaded.");
                }

                sourceSchemas.Add(schemaInfo);

                logger.LogInformation(
                    "Discovered source schema {Name} for resource {ResourceName}",
                    schemaInfo.Name,
                    schemaInfo.ResourceName);
            }
        }
        catch
        {
            foreach (var sourceSchema in sourceSchemas)
            {
                sourceSchema.SchemaSettings.Dispose();
            }

            throw;
        }

        return sourceSchemas;
    }

    [SuppressMessage(
        "Trimming",
        "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' "
        + "in call to target method. The return value of the source method does not have matching annotations.")]
    [SuppressMessage("ReSharper", "UnusedVariable")]
    private static List<IResourceWithEndpoints> GetReferencedResources(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel)
    {
        var referencedResourceNames = new HashSet<string>();

        foreach (var annotation in compositionResource.Annotations)
        {
            switch (annotation)
            {
                case ResourceRelationshipAnnotation rel:
                    referencedResourceNames.Add(rel.Resource.Name);
                    break;

                case var endpointRef when annotation.GetType().Name == "EndpointReferenceAnnotation":
                    var targetResourceProp = annotation.GetType().GetProperty("Resource");
                    if (targetResourceProp?.GetValue(annotation) is IResource targetResource)
                    {
                        referencedResourceNames.Add(targetResource.Name);
                    }
                    break;
            }
        }

        return appModel.Resources
            .OfType<IResourceWithEndpoints>()
            .Where(r => referencedResourceNames.Contains(r.Name))
            .ToList();
    }

    private async Task<SourceSchemaInfo?> GetSourceSchemaInfoAsync(
        IResourceWithEndpoints resource,
        GraphQLSourceSchemaAnnotation sourceSchemaSettings,
        CancellationToken cancellationToken)
    {
        switch (sourceSchemaSettings.Location)
        {
            case SourceSchemaLocationType.SchemaEndpoint:
                return await GetSourceSchemaFromEndpointAsync(resource, sourceSchemaSettings, cancellationToken);

            case SourceSchemaLocationType.ProjectDirectory:
                return await GetSourceSchemaFromFileAsync(resource, sourceSchemaSettings, cancellationToken);

            case SourceSchemaLocationType.CommandLineExport:
                return await GetSourceSchemaFromCommandLineAsync(
                    resource,
                    sourceSchemaSettings,
                    cancellationToken);

            default:
                logger.LogWarning(
                    "Unknown schema location type {LocationType} for {ResourceName}",
                    sourceSchemaSettings.Location,
                    resource.Name);
                return null;
        }
    }

    private async Task<SourceSchemaInfo> GetSourceSchemaFromCommandLineAsync(
        IResourceWithEndpoints resource,
        GraphQLSourceSchemaAnnotation annotation,
        CancellationToken cancellationToken)
    {
        var outputDirectory = IOPath.Combine(
            IOPath.GetTempPath(),
            "hotchocolate-fusion-aspire",
            Guid.NewGuid().ToString("N"));

        try
        {
            var result = await CommandLineSchemaExporter.ExportAsync(
                resource,
                annotation,
                outputDirectory,
                cancellationToken);
            var schemaText = await File.ReadAllTextAsync(
                result.SchemaPath,
                cancellationToken);
            var settings = JsonDocument.Parse(
                await File.ReadAllTextAsync(result.SettingsPath, cancellationToken));
            var ownershipTransferred = false;

            try
            {
                var configuration = ReadEndpointConfiguration(
                    resource.Name,
                    annotation.SourceSchemaName,
                    settings);
                GraphQLSourceSchemaValidator.Validate(
                    resource.Name,
                    configuration,
                    schemaText);

                var sourceSchema = new SourceSchemaInfo
                {
                    Name = configuration.SourceSchemaName,
                    ResourceName = resource.Name,
                    Schema = new SourceSchemaText(configuration.SourceSchemaName, schemaText),
                    SchemaSettings = settings
                };

                ownershipTransferred = true;
                return sourceSchema;
            }
            finally
            {
                if (!ownershipTransferred)
                {
                    settings.Dispose();
                }
            }
        }
        finally
        {
            try
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, recursive: true);
                }
            }
            catch (IOException exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not remove schema export directory for {ResourceName}.",
                    resource.Name);
            }
            catch (UnauthorizedAccessException exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not remove schema export directory for {ResourceName}.",
                    resource.Name);
            }
        }
    }

    private async Task<SourceSchemaInfo?> GetSourceSchemaFromEndpointAsync(
        IResourceWithEndpoints resource,
        GraphQLSourceSchemaAnnotation annotation,
        CancellationToken cancellationToken)
    {
        // For endpoint schemas, look for "schema-settings.json" in the project directory.
        var schemaSettings = await GetSourceSchemaSettingsAsync(
            resource,
            "schema-settings.json",
            cancellationToken);
        if (schemaSettings == null)
        {
            logger.LogWarning("Could not find schema-settings.json for {ResourceName}", resource.Name);
            return null;
        }

        var ownershipTransferred = false;

        try
        {
            var endpointConfiguration = ReadEndpointConfiguration(
                resource.Name,
                annotation.SourceSchemaName,
                schemaSettings);
            var schemaUrl = resource.GetGraphQLSchemaUrl(endpointConfiguration.DefaultPath);

            if (schemaUrl is null)
            {
                logger.LogWarning("Could not determine schema URL for {ResourceName}", resource.Name);
                return null;
            }

            var schemaText = await FetchSchemaFromEndpointAsync(
                endpointConfiguration.SourceSchemaName,
                schemaUrl,
                endpointConfiguration.Protocol,
                cancellationToken);
            if (schemaText == null)
            {
                return null;
            }

            GraphQLSourceSchemaValidator.Validate(
                resource.Name,
                endpointConfiguration,
                schemaText);

            var sourceSchema = new SourceSchemaInfo
            {
                Name = endpointConfiguration.SourceSchemaName,
                ResourceName = resource.Name,
                HttpEndpointUrl = new Uri(schemaUrl),
                AllocatedHttpEndpointUrl = resource.GetAllocatedHttpEndpointUrl(),
                Schema = new SourceSchemaText(endpointConfiguration.SourceSchemaName, schemaText),
                SchemaSettings = schemaSettings
            };

            ownershipTransferred = true;
            return sourceSchema;
        }
        finally
        {
            if (!ownershipTransferred)
            {
                schemaSettings.Dispose();
            }
        }
    }

    internal static SchemaEndpointConfiguration ReadEndpointConfiguration(
        string resourceName,
        string? configuredSourceSchemaName,
        JsonDocument schemaSettings)
    {
        ArgumentException.ThrowIfNullOrEmpty(resourceName);
        ArgumentNullException.ThrowIfNull(schemaSettings);

        var root = schemaSettings.RootElement;

        if (root.ValueKind is not JsonValueKind.Object)
        {
            if (!ApolloFederationSourceSchemaSettings.TryReadVersion(
                configuredSourceSchemaName ?? resourceName,
                root,
                out _,
                out var errorMessage))
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        if (!root.TryGetProperty("name", out var name)
            || name.ValueKind is not JsonValueKind.String
            || string.IsNullOrWhiteSpace(name.GetString()))
        {
            throw new InvalidOperationException(
                $"Schema settings for resource '{resourceName}' must specify a non-empty string 'name'.");
        }

        var sourceSchemaName = name.GetString()!;

        if (configuredSourceSchemaName?.Equals(sourceSchemaName, StringComparison.Ordinal) is false)
        {
            throw new InvalidOperationException(
                $"The configured source schema name '{configuredSourceSchemaName}' for resource "
                + $"'{resourceName}' does not match schema-settings.json name '{sourceSchemaName}'.");
        }

        if (!ApolloFederationSourceSchemaSettings.TryReadVersion(
            sourceSchemaName,
            root,
            out var version,
            out var versionErrorMessage))
        {
            throw new InvalidOperationException(versionErrorMessage);
        }

        return new(sourceSchemaName, version);
    }

    private async Task<SourceSchemaInfo?> GetSourceSchemaFromFileAsync(
        IResourceWithEndpoints resource,
        GraphQLSourceSchemaAnnotation annotation,
        CancellationToken cancellationToken)
    {
        var sourceSchemaName = annotation.SourceSchemaName ?? resource.Name;

        var schemaPaths = GraphQLResourceModel.GetProjectSchemaPaths(
            resource,
            annotation);
        var schemaPath = schemaPaths.SchemaPath;

        if (IsExtensionsSchemaPath(schemaPath))
        {
            logger.LogWarning(
                "Schema extensions file '{SchemaPath}' cannot be used as a source schema file. Provide the base schema file instead.",
                schemaPath);
            return null;
        }

        var schemaFromFile = await ReadSchemaFromProjectDirectoryAsync(
            resource,
            schemaPath,
            cancellationToken);
        if (schemaFromFile is not { } schemaFiles)
        {
            return null;
        }

        var schemaSettings = await GetSourceSchemaSettingsAsync(
            resource,
            schemaPaths.SettingsPath,
            cancellationToken);
        if (schemaSettings == null)
        {
            return null;
        }

        var ownershipTransferred = false;

        try
        {
            var configuration = ReadEndpointConfiguration(
                resource.Name,
                sourceSchemaName,
                schemaSettings);
            GraphQLSourceSchemaValidator.Validate(
                resource.Name,
                configuration,
                schemaFiles.Schema,
                schemaFiles.Extensions);

            var sourceSchema = new SourceSchemaInfo
            {
                Name = configuration.SourceSchemaName,
                ResourceName = resource.Name,
                // No schema download endpoint for file-based schemas
                HttpEndpointUrl = null,
                AllocatedHttpEndpointUrl = resource.GetAllocatedHttpEndpointUrl(),
                Schema = new SourceSchemaText(
                    configuration.SourceSchemaName,
                    schemaFiles.Schema,
                    schemaFiles.Extensions),
                SchemaSettings = schemaSettings
            };

            ownershipTransferred = true;
            return sourceSchema;
        }
        finally
        {
            if (!ownershipTransferred)
            {
                schemaSettings.Dispose();
            }
        }
    }

    private async Task<JsonDocument?> GetSourceSchemaSettingsAsync(
        IResourceWithEndpoints resource,
        string settingsFileName,
        CancellationToken cancellationToken)
    {
        try
        {
            var projectPath = GetProjectPath(resource);
            if (projectPath == null)
            {
                logger.LogWarning("Could not determine project path for {ResourceName}", resource.Name);
                return null;
            }

            var projectDirectory = IOPath.GetDirectoryName(projectPath);
            var settingsFile = IOPath.IsPathRooted(settingsFileName)
                ? settingsFileName
                : IOPath.Combine(projectDirectory!, settingsFileName);

            if (!File.Exists(settingsFile))
            {
                logger.LogWarning("Schema settings file not found: {SettingsFile}", settingsFile);
                return null;
            }

            var settingsJson = await File.ReadAllTextAsync(settingsFile, cancellationToken);
            return JsonDocument.Parse(settingsJson);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to read schema settings file {SettingsFileName} for {ResourceName}",
                settingsFileName,
                resource.Name);
            return null;
        }
    }

    private async Task<string?> FetchSchemaFromEndpointAsync(
        string sourceSchemaName,
        string schemaUrl,
        SchemaEndpointProtocol protocol,
        CancellationToken cancellationToken)
    {
        var endpoint = new Uri(schemaUrl);

        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        return await FetchSchemaFromEndpointAsync(
            sourceSchemaName,
            endpoint,
            protocol,
            httpClient,
            FetchMaxRetries,
            s_fetchRetryDelay,
            cancellationToken);
    }

    internal async Task<string?> FetchSchemaFromEndpointAsync(
        string sourceSchemaName,
        Uri endpoint,
        SchemaEndpointProtocol protocol,
        HttpClient httpClient,
        int maxRetries,
        TimeSpan retryDelay,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceSchemaName);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetries, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(retryDelay, TimeSpan.Zero);

        logger.LogDebug("Waiting for schema service {SourceSchemaName}", sourceSchemaName);

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                if (protocol is SchemaEndpointProtocol.ApolloFederation)
                {
                    return await ApolloFederationSchemaFetcher.FetchAsync(
                        httpClient,
                        sourceSchemaName,
                        endpoint,
                        cancellationToken);
                }

                return await DefaultSchemaFetcher.FetchAsync(
                    httpClient,
                    sourceSchemaName,
                    endpoint,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} timed out (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }
            catch (HttpRequestException exception) when (exception.StatusCode is null)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} was unavailable (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }
            catch (IOException)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} was unavailable (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }
            // The DCP proxy keeps an endpoint open while the source process starts or
            // restarts, so a fetch can observe transient server errors instead of
            // connection failures. Server errors are retried, everything below 500
            // fails immediately.
            catch (HttpRequestException exception) when (
                exception.StatusCode >= HttpStatusCode.InternalServerError)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} returned a transient server error (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }
            catch (SchemaFetchRequestException exception) when (
                exception.StatusCode >= HttpStatusCode.InternalServerError)
            {
                logger.LogDebug(
                    "Schema service {SourceSchemaName} returned a transient server error (attempt {Attempt}/{MaxRetries})",
                    sourceSchemaName,
                    i + 1,
                    maxRetries);
            }

            if (i + 1 < maxRetries)
            {
                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        logger.LogWarning(
            "Schema service {SourceSchemaName} failed to become ready after {MaxRetries} attempts",
            sourceSchemaName,
            maxRetries);
        return null;
    }

    private async Task<(string Schema, string? Extensions)?> ReadSchemaFromProjectDirectoryAsync(
        IResourceWithEndpoints resource,
        string? fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the project directory from the resource metadata
            var projectPath = GetProjectPath(resource);
            if (projectPath == null)
            {
                logger.LogWarning("Could not determine project path for {ResourceName}", resource.Name);
                return null;
            }

            var projectDirectory = IOPath.GetDirectoryName(projectPath);
            var schemaPath = fileName ?? "schema.graphqls";
            var schemaFile = IOPath.IsPathRooted(schemaPath)
                ? schemaPath
                : IOPath.Combine(projectDirectory!, schemaPath);

            if (!File.Exists(schemaFile))
            {
                logger.LogWarning("Schema file not found: {SchemaFile}", schemaFile);
                return null;
            }

            var schemaText = await File.ReadAllTextAsync(schemaFile, cancellationToken);

            var extensionsFile = IOPath.Combine(
                IOPath.GetDirectoryName(schemaFile)!,
                IOPath.GetFileNameWithoutExtension(schemaFile)
                + "-extensions"
                + IOPath.GetExtension(schemaFile));

            string? extensionsText = null;
            if (File.Exists(extensionsFile))
            {
                extensionsText = await File.ReadAllTextAsync(extensionsFile, cancellationToken);
            }

            return (schemaText, extensionsText);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read schema file for {ResourceName}", resource.Name);
            return null;
        }
    }

    private string? GetProjectPath(IResourceWithEndpoints resource)
    {
        try
        {
            return GraphQLResourceModel.GetProjectPath(resource);
        }
        catch (InvalidOperationException)
        {
            logger.LogWarning(
                "Could not find project metadata for resource {ResourceName}",
                resource.Name);
            return null;
        }
    }

    private async Task<SchemaCompositionOutcome> ComposeSchemaAsync(
        string gatewayName,
        string archivePath,
        NitroGatewaySeed? seed,
        List<SourceSchemaInfo> sourceSchemas,
        GraphQLSchemaCompositionAnnotation settings,
        ILogger compositionLogger,
        bool captureGatewaySchema,
        CancellationToken cancellationToken)
    {
        var tempArchivePath = IOPath.Combine(IOPath.GetTempPath(), IOPath.GetRandomFileName());

        try
        {
            // A gateway that composes against a fusion configuration from Nitro builds on that
            // configuration alone. What the previous composition wrote is never an input, so a
            // source schema that disappeared upstream also disappears from this composition.
            if (seed is null && File.Exists(archivePath))
            {
                File.Copy(archivePath, tempArchivePath);
            }

            if (await AspireCompositionHelper.TryComposeAsync(
                tempArchivePath,
                seed?.FilePath,
                [.. sourceSchemas],
                settings.Settings,
                seed?.Stage,
                compositionLogger,
                cancellationToken))
            {
                if (seed is not null)
                {
                    await LogSeedVintageAsync(
                        gatewayName,
                        tempArchivePath,
                        seed,
                        sourceSchemas,
                        compositionLogger,
                        cancellationToken);
                }

                // The gateway keeps read handles on the archive while it is running, which can
                // surface as a transient IOException when the archive is replaced.
                await CopyArchiveWithRetryAsync(
                    () => File.Copy(tempArchivePath, archivePath, true),
                    archivePath,
                    ArchiveCopyMaxAttempts,
                    s_archiveCopyRetryDelay,
                    cancellationToken);

                byte[]? gatewaySchema = null;
                if (captureGatewaySchema)
                {
                    try
                    {
                        gatewaySchema = await ReadGatewaySchemaAsync(
                            tempArchivePath,
                            cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        compositionLogger.LogWarning(
                            exception,
                            "The gateway schema for {ResourceName} could not be captured for "
                            + "observational Nitro validation. The installed archive is unaffected.",
                            gatewayName);
                    }
                }

                return new SchemaCompositionOutcome(true, gatewaySchema);
            }
        }
        finally
        {
            if (File.Exists(tempArchivePath))
            {
                File.Delete(tempArchivePath);
            }
        }

        return SchemaCompositionOutcome.Failed;
    }

    private static async Task<byte[]> ReadGatewaySchemaAsync(
        string archivePath,
        CancellationToken cancellationToken)
    {
        using var archive = FusionArchive.Open(archivePath);
        using var configuration = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            cancellationToken);

        if (configuration is null)
        {
            throw new InvalidOperationException(
                "The composed archive does not contain a supported gateway schema.");
        }

        await using var schema = await configuration.OpenReadSchemaAsync(cancellationToken);
        var buffer = GC.AllocateUninitializedArray<byte>(checked((int)schema.Length));
        await schema.ReadExactlyAsync(buffer, cancellationToken);
        return buffer;
    }

    /// <summary>
    /// Reports which fusion configuration a gateway composed against, how old it is and which
    /// source schemas it contributed, on the console of the gateway. A configuration that could
    /// not be refreshed is reported as a warning.
    /// </summary>
    private static async Task LogSeedVintageAsync(
        string gatewayName,
        string archivePath,
        NitroGatewaySeed seed,
        List<SourceSchemaInfo> localSourceSchemas,
        ILogger compositionLogger,
        CancellationToken cancellationToken)
    {
        var externalSourceSchemas = await DescribeExternalSourceSchemasAsync(
            archivePath,
            [.. localSourceSchemas.Select(sourceSchema => sourceSchema.Name)],
            cancellationToken);
        var downloadedAt = seed.DownloadedAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") + "Z";

        if (seed.IsFresh)
        {
            compositionLogger.LogInformation(
                "{ResourceName} composed against the fusion configuration of the Nitro api "
                + "{ApiId} for the stage {Stage} that was downloaded at {DownloadedAt}. Source "
                + "schemas from Nitro: {ExternalSourceSchemas}.",
                gatewayName,
                seed.ApiId,
                seed.Stage,
                downloadedAt,
                externalSourceSchemas);
            return;
        }

        compositionLogger.LogWarning(
            "{ResourceName} composed against a fusion configuration that could NOT be refreshed. "
            + "The configuration of the Nitro api {ApiId} for the stage {Stage} was downloaded at "
            + "{DownloadedAt} and may be out of date, run 'nitro login' when the sign-in expired. "
            + "Source schemas from Nitro: {ExternalSourceSchemas}.",
            gatewayName,
            seed.ApiId,
            seed.Stage,
            downloadedAt,
            externalSourceSchemas);
    }

    /// <summary>
    /// Describes the source schemas of a composed archive that the distributed application does
    /// not run itself, together with the URL the gateway reaches them at.
    /// </summary>
    private static async Task<string> DescribeExternalSourceSchemasAsync(
        string archivePath,
        HashSet<string> localSourceSchemas,
        CancellationToken cancellationToken)
    {
        using var archive = FusionArchive.Open(archivePath);
        using var configuration = await archive.TryGetGatewayConfigurationAsync(
            WellKnownVersions.LatestGatewayFormatVersion,
            cancellationToken);

        if (configuration is null
            || !configuration.Settings.RootElement.TryGetProperty(
                "sourceSchemas",
                out var sourceSchemas)
            || sourceSchemas.ValueKind is not JsonValueKind.Object)
        {
            return "none";
        }

        var descriptions = new List<string>();

        foreach (var sourceSchema in sourceSchemas.EnumerateObject())
        {
            if (localSourceSchemas.Contains(sourceSchema.Name))
            {
                continue;
            }

            descriptions.Add($"{sourceSchema.Name} ({ReadHttpUrl(sourceSchema.Value)})");
        }

        return descriptions.Count == 0 ? "none" : string.Join(", ", descriptions);
    }

    private static string ReadHttpUrl(JsonElement sourceSchemaSettings)
        => sourceSchemaSettings.TryGetProperty("transports", out var transports)
            && transports.TryGetProperty("http", out var http)
            && http.TryGetProperty("url", out var url)
            && url.ValueKind is JsonValueKind.String
                ? url.GetString()!
                : "no HTTP transport URL";

    private sealed record SchemaCompositionOutcome(bool Success, byte[]? GatewaySchema)
    {
        public static SchemaCompositionOutcome Failed { get; } = new(false, null);

        public static SchemaCompositionOutcome SucceededWithoutSchema { get; } = new(true, null);
    }

    /// <summary>
    /// Creates the logger that reports to the application host log and to the console log of a
    /// resource at the same time, so that its messages are visible on the resource itself.
    /// </summary>
    private CompositeLogger CreateGatewayLogger(IResource resource)
        => new(logger, resourceLoggerService.GetLogger(resource));

    internal async Task CopyArchiveWithRetryAsync(
        Action copyArchive,
        string archivePath,
        int maxAttempts,
        TimeSpan retryDelay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(copyArchive);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                copyArchive();
                return;
            }
            catch (IOException exception) when (attempt < maxAttempts)
            {
                logger.LogDebug(
                    exception,
                    "Could not replace the fusion archive {ArchivePath} (attempt {Attempt}/{MaxAttempts})",
                    archivePath,
                    attempt,
                    maxAttempts);
            }

            await Task.Delay(retryDelay, cancellationToken);
        }
    }

    private static bool IsExtensionsSchemaPath(string filePath)
        => IOPath.GetFileNameWithoutExtension(filePath).EndsWith(
            "-extensions",
            StringComparison.OrdinalIgnoreCase);
}
