using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

internal sealed class SchemaComposition(
    ResourceNotificationService resourceNotificationService,
    ResourceLoggerService resourceLoggerService,
    IHostApplicationLifetime lifetime,
    ILogger<SchemaComposition> logger)
    : IDistributedApplicationEventingSubscriber
{
    private const int FetchMaxRetries = 15;
    private const int ArchiveCopyMaxAttempts = 5;
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

        // All subscriptions must exist before any resource starts. A fast resource can publish
        // its initial ResourceReadyEvent early, and events are not replayed to late subscribers,
        // so subscribing any later would misclassify the first restart of such a resource as its
        // initial start.
        eventing.Subscribe<BeforeStartEvent>((@event, _) =>
        {
            var compositionResources = @event.Model.GetGraphQLCompositionResources().ToList();

            if (compositionResources.Count == 0)
            {
                logger.LogDebug("No resources found that need GraphQL schema composition");
                return Task.CompletedTask;
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
            return Task.CompletedTask;
        });

        return Task.CompletedTask;
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
        await compositionGate.WaitAsync(cancellationToken);

        try
        {
            logger.LogInformation(
                "Composing the GraphQL schema for {ResourceName} before it starts.",
                compositionResource.Name);

            Exception? failure = null;

            try
            {
                await WaitForSourceSchemaResourcesReadyAsync(
                    compositionResource,
                    appModel,
                    cancellationToken);

                if (await ComposeSchemaAsync(compositionResource, appModel, cancellationToken))
                {
                    return;
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

            var message = failure is null
                ? $"The GraphQL schema composition for '{compositionResource.Name}' failed."
                : $"The GraphQL schema composition for '{compositionResource.Name}' failed: {failure.Message}";

            logger.LogError(failure, "{Message}", message);
            resourceLoggerService.GetLogger(compositionResource).LogError(failure, "{Message}", message);

            throw failure is null
                ? new DistributedApplicationException(message)
                : new DistributedApplicationException(message, failure);
        }
        finally
        {
            compositionGate.Release();
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
        await compositionGate.WaitAsync(cancellationToken);

        try
        {
            await RecomposeSchemaAsync(compositionResource, appModel, cancellationToken);
        }
        finally
        {
            compositionGate.Release();
        }
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

    private async Task RecomposeSchemaAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Recomposing GraphQL schema for {ResourceName}...",
            compositionResource.Name);

        if (await ComposeSchemaAsync(compositionResource, appModel, cancellationToken))
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
    }

    private async Task<bool> ComposeSchemaAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken)
    {
        var settings = compositionResource.GetCompositionSettings();

        if (settings is null)
        {
            return true;
        }

        // Composition problems are reported to the application host log and to the console
        // log of the gateway resource so that failures are visible on the resource itself.
        var compositionLogger = new CompositeLogger(
            logger,
            resourceLoggerService.GetLogger(compositionResource));

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
                return true;
            }

            try
            {
                var gatewayDirectory = GetProjectPath(compositionResource)!;
                var archivePath = IOPath.Combine(IOPath.GetDirectoryName(gatewayDirectory)!, settings.OutputFileName);
                return await ComposeSchemaAsync(
                    archivePath,
                    sourceSchemas,
                    settings,
                    compositionLogger,
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

        return false;
    }

    internal async Task<List<SourceSchemaInfo>> DiscoverReferencedSourceSchemasAsync(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel appModel,
        CancellationToken cancellationToken)
    {
        var sourceSchemas = new List<SourceSchemaInfo>();

        // Get all resources referenced by the composition resource
        var referencedResources = GetReferencedResources(compositionResource, appModel);

        logger.LogInformation(
            "Found {Count} referenced resources for {ResourceName}",
            referencedResources.Count, compositionResource.Name);

        try
        {
            foreach (var referencedResource in referencedResources)
            {
                if (!referencedResource.HasGraphQLSchema())
                {
                    logger.LogDebug(
                        "Resource {ResourceName} does not have a GraphQL schema, skipping",
                        referencedResource.Name);
                    continue;
                }

                var schemaInfo = await GetSourceSchemaInfoAsync(referencedResource, cancellationToken);
                if (schemaInfo is null)
                {
                    // Composing without this source would either drop it from the composed
                    // schema or silently carry its previous schema forward from the existing
                    // archive, so the composition must fail instead. On startup this marks
                    // the gateway as failed to start, on a recomposition the gateway keeps
                    // the previous schema.
                    throw new InvalidOperationException(
                        $"The source schema for resource '{referencedResource.Name}' could not be loaded.");
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
        CancellationToken cancellationToken)
    {
        var sourceSchemaSettings = resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>().FirstOrDefault();
        if (sourceSchemaSettings is null)
        {
            return null;
        }

        switch (sourceSchemaSettings.Location)
        {
            case SourceSchemaLocationType.SchemaEndpoint:
                return await GetSourceSchemaFromEndpointAsync(resource, sourceSchemaSettings, cancellationToken);

            case SourceSchemaLocationType.ProjectDirectory:
                return await GetSourceSchemaFromFileAsync(resource, sourceSchemaSettings, cancellationToken);

            default:
                logger.LogWarning(
                    "Unknown schema location type {LocationType} for {ResourceName}",
                    sourceSchemaSettings.Location,
                    resource.Name);
                return null;
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

            var sourceSchema = new SourceSchemaInfo
            {
                Name = endpointConfiguration.SourceSchemaName,
                ResourceName = resource.Name,
                HttpEndpointUrl = new Uri(schemaUrl),
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
        var sourceSchemaName = resource.GetGraphQLSourceSchemaName() ?? resource.Name;

        var schemaPath = annotation.SchemaPath ?? "schema.graphql";

        if (IsExtensionsSchemaPath(schemaPath))
        {
            logger.LogWarning(
                "Schema extensions file '{SchemaPath}' cannot be used as a source schema file. Provide the base schema file instead.",
                schemaPath);
            return null;
        }

        var schemaFromFile = await ReadSchemaFromProjectDirectoryAsync(resource, schemaPath, cancellationToken);
        if (schemaFromFile is not { } schemaFiles)
        {
            return null;
        }

        // For file schemas, settings file is named after the schema file
        // e.g., "foo.graphql" -> "foo-settings.json"
        var settingsFileName = $"{IOPath.GetFileNameWithoutExtension(schemaPath)}-settings.json";

        var schemaSettings = await GetSourceSchemaSettingsAsync(resource, settingsFileName, cancellationToken);
        if (schemaSettings == null)
        {
            return null;
        }

        return new SourceSchemaInfo
        {
            Name = sourceSchemaName,
            ResourceName = resource.Name,
            HttpEndpointUrl = null, // No HTTP endpoint for file-based schemas
            Schema = new SourceSchemaText(sourceSchemaName, schemaFiles.Schema, schemaFiles.Extensions),
            SchemaSettings = schemaSettings
        };
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
            var settingsFile = IOPath.Combine(projectDirectory!, settingsFileName);

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
            var schemaFile = IOPath.Combine(projectDirectory!, fileName ?? "schema.graphql");

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

    [SuppressMessage(
        "Trimming",
        "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' "
        + "in call to target method. The return value of the source method does not have matching annotations.")]
    private string? GetProjectPath(IResourceWithEndpoints resource)
    {
        // Check if this is a ProjectResource
        if (resource is not ProjectResource projectResource)
        {
            return null;
        }

        // Get the project metadata from the ProjectResource
        // The metadata is typically stored as an annotation or property
        var metadataAnnotation = projectResource.Annotations
            .FirstOrDefault(a => a.GetType().GetInterfaces().Contains(typeof(IProjectMetadata)));

        if (metadataAnnotation is IProjectMetadata projectMetadata)
        {
            return projectMetadata.ProjectPath;
        }

        // Alternative approach: look for the metadata in the resource's type or properties
        // Sometimes the metadata might be accessible through reflection on the resource itself
        var metadataProperty = projectResource.GetType()
            .GetProperties()
            .FirstOrDefault(p => p.PropertyType.GetInterfaces().Contains(typeof(IProjectMetadata)));

        if (metadataProperty != null)
        {
            var metadata = metadataProperty.GetValue(projectResource) as IProjectMetadata;
            return metadata?.ProjectPath;
        }

        logger.LogWarning("Could not find project metadata for resource {ResourceName}", resource.Name);
        return null;
    }

    private async Task<bool> ComposeSchemaAsync(
        string archivePath,
        List<SourceSchemaInfo> sourceSchemas,
        GraphQLSchemaCompositionAnnotation settings,
        ILogger compositionLogger,
        CancellationToken cancellationToken)
    {
        var tempArchivePath = IOPath.Combine(IOPath.GetTempPath(), IOPath.GetRandomFileName());

        try
        {
            if (File.Exists(archivePath))
            {
                File.Copy(archivePath, tempArchivePath);
            }

            if (await AspireCompositionHelper.TryComposeAsync(
                tempArchivePath,
                [.. sourceSchemas],
                settings.Settings,
                compositionLogger,
                cancellationToken))
            {
                // The gateway keeps read handles on the archive while it is running, which can
                // surface as a transient IOException when the archive is replaced.
                await CopyArchiveWithRetryAsync(
                    () => File.Copy(tempArchivePath, archivePath, true),
                    archivePath,
                    ArchiveCopyMaxAttempts,
                    s_archiveCopyRetryDelay,
                    cancellationToken);
                return true;
            }
        }
        finally
        {
            if (File.Exists(tempArchivePath))
            {
                File.Delete(tempArchivePath);
            }
        }

        return false;
    }

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
