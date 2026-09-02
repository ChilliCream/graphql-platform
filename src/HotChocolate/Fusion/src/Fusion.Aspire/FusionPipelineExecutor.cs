using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using HotChocolate.Fusion.Aspire.Nitro;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES004

internal sealed class FusionPipelineExecutor
{
    private static readonly TimeSpan s_readinessRetryDelay =
        TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions s_jsonOptions = new(
        JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly FusionPipelineMemoryLimits _memoryLimits;
    // The token refresh is a short request against the Nitro API that every pipeline step can
    // make, so the steps share one client instead of creating one per invocation.
    private static readonly HttpClient s_tokenRefreshHttpClient = new();

    private readonly Func<HttpClient> _schemaHttpClientFactory;

    internal FusionPipelineExecutor(FusionPipelineMemoryLimits memoryLimits)
        : this(
            memoryLimits,
            static () => new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            })
    {
    }

    internal FusionPipelineExecutor(
        FusionPipelineMemoryLimits memoryLimits,
        Func<HttpClient> schemaHttpClientFactory)
    {
        _memoryLimits = memoryLimits;
        _schemaHttpClientFactory = schemaHttpClientFactory
            ?? throw new ArgumentNullException(nameof(schemaHttpClientFactory));
    }

    public static FusionPipelineExecutor Instance { get; } =
        new(FusionPipelineMemoryLimits.Default);

    public async Task CreateArtifactsAsync(PipelineStepContext context)
    {
        var apis = FusionPipeline.SelectApis(context.Model);

        if (apis.Count == 0)
        {
            return;
        }

        var composition = FusionPipeline.GetCompositionResource(context.Model);
        var sources = GraphQLResourceModel.GetReferencedSourceSchemas(
            composition,
            context.Model);

        if (sources.Count == 0)
        {
            throw new InvalidOperationException(
                $"Fusion composition resource '{composition.Name}' has no declared source schemas.");
        }

        _ = GetSourceNames(context.Model);

        var output = context.Services
            .GetRequiredService<IPipelineOutputService>()
            .GetOutputDirectory();
        var logger = context.Services
            .GetRequiredService<ILogger<SchemaComposition>>();

        var invocation = ResolveInvocation(context);
        foreach (var api in apis)
        {
            await CreateDeploymentArtifactsAsync(
                api,
                FusionPipeline.GetStages(context.Model, api),
                sources,
                invocation.Tag,
                output,
                context.Services,
                logger,
                context.CancellationToken);
        }
    }

    public async Task VerifyReadinessAsync(
        PipelineStepContext context,
        FusionPipelineSession session)
    {
        var deployments = session.GetDeployments();

        if (deployments.Count == 0)
        {
            return;
        }

        var compositionResource = FusionPipeline.GetCompositionResource(
            context.Model);
        var composition = GraphQLResourceModel.GetComposition(
            compositionResource);
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        try
        {
            foreach (var deployment in deployments)
            {
                var state = session.GetState(deployment);
                await ValidateSessionStateAsync(
                    state,
                    deployment,
                    context,
                    context.CancellationToken);
                ValidateCompositionState(
                    state,
                    deployment,
                    composition.Settings);
                VerifyMemoryDigest(
                    state.FusionArchive,
                    state.FusionArchiveSha256
                        ?? throw new InvalidOperationException(
                            "The composed Fusion archive has no digest."),
                    "composed Fusion archive");
                await using var archiveStream = new MemoryStream(
                    state.FusionArchive,
                    writable: false);
                using var archive = FusionArchive.Open(archiveStream);
                using var configuration =
                    await archive.TryGetGatewayConfigurationAsync(
                        WellKnownVersions.LatestGatewayFormatVersion,
                        context.CancellationToken)
                    ?? throw new InvalidDataException(
                        "The composed Fusion archive contains no gateway configuration.");

                foreach (var (name, endpoint) in GetTransportEndpoints(
                    configuration.Settings))
                {
                    RejectLoopbackEndpoint(endpoint);
                    await WaitForReadinessAsync(
                        httpClient,
                        name,
                        endpoint,
                        NitroStageResource.OperationTimeout,
                        s_readinessRetryDelay,
                        context.CancellationToken);
                }
            }
        }
        catch
        {
            session.Clear();
            throw;
        }
    }

    public async Task UploadAsync(PipelineStepContext context)
    {
        var artifacts = await MaterializeArchivesAsync(context);
        if (artifacts.Count == 0)
        {
            return;
        }

        var workflow = context.Services.GetRequiredService<FusionDeploymentWorkflow>();

        foreach (var group in artifacts.GroupBy(artifact => artifact.Target))
        {
            var target = await ResolveTargetAsync(
                group.Key,
                context,
                context.CancellationToken);

            foreach (var artifact in group)
            {
                await workflow.ReconcileSourceSchemaAsync(
                    target,
                    new FusionSourceSchemaUpload(
                        artifact.Name,
                        artifact.Version,
                        artifact.ArchivePath,
                        artifact.Sha256),
                    context.CancellationToken);
            }
        }
    }

    public async Task PreflightAsync(
        PipelineStepContext context,
        FusionPipelineSession session)
    {
        var invocation = ResolveInvocation(context);
        var deployments = FusionPipeline.SelectStages(
            context.Model,
            invocation.RequireStage());
        if (deployments.Count == 0)
        {
            return;
        }

        var workflow = context.Services
            .GetRequiredService<FusionDeploymentWorkflow>();
        var sourceNames = GetSourceNames(context.Model);
        var preparedStates = new List<(
            NitroStageResource Deployment,
            FusionDeploymentSessionState State)>(deployments.Count);
        var transferred = false;

        try
        {
            foreach (var deployment in deployments)
            {
                var tag = invocation.Tag;
                var target = await ResolveTargetAsync(
                    deployment,
                    context,
                    context.CancellationToken);
                var sources = new List<FusionSessionSourceIdentity>(
                    sourceNames.Count);

                foreach (var sourceName in sourceNames)
                {
                    using var download = await DownloadExactSourceAsync(
                        workflow,
                        target,
                        deployment,
                        sourceName,
                        tag,
                        context.CancellationToken);
                    ValidateSourceSize(
                        download.Archive.Length,
                        sourceName,
                        tag);
                    byte[]? archive = download.Archive.ToArray();
                    try
                    {
                        var contentSha256 =
                            await ValidateCanonicalDigestAsync(
                                download,
                                archive,
                                sourceName,
                                tag,
                                context.CancellationToken);
                        sources.Add(
                            new FusionSessionSourceIdentity(
                                sourceName,
                                tag,
                                contentSha256));
                    }
                    finally
                    {
                        if (archive is not null)
                        {
                            Array.Clear(archive);
                        }
                    }
                }

                preparedStates.Add(
                    (deployment,
                    new FusionDeploymentSessionState(
                        tag,
                        deployment.Api.Nitro.CloudUrl!,
                        deployment.Api.ApiId!,
                        sources)));
            }

            session.SetAll(preparedStates);
            transferred = true;
        }
        catch
        {
            session.Clear();
            throw;
        }
        finally
        {
            if (!transferred)
            {
                foreach (var (_, state) in preparedStates)
                {
                    state.Dispose();
                }
            }
        }
    }

    public async Task DownloadAsync(
        PipelineStepContext context,
        FusionPipelineSession session)
    {
        var deployments = session.GetDeployments();
        if (deployments.Count == 0)
        {
            return;
        }

        var workflow = context.Services
            .GetRequiredService<FusionDeploymentWorkflow>();
        long totalSourceArchiveBytes = 0;

        try
        {
            foreach (var deployment in deployments)
            {
                var state = session.GetState(deployment);
                await ValidatePreflightStateAsync(
                    state,
                    deployment,
                    context,
                    context.CancellationToken);
                var target = await ResolveTargetAsync(
                    deployment,
                    context,
                    context.CancellationToken);
                var sources = new List<FusionSessionSource>(
                    state.SourceIdentities.Count);
                var sourceListTransferred = false;

                try
                {
                    foreach (var identity in state.SourceIdentities)
                    {
                        using var download = await DownloadExactSourceAsync(
                            workflow,
                            target,
                            deployment,
                            identity.Name,
                            identity.Version,
                            context.CancellationToken);
                        ValidateSourceSize(
                            download.Archive.Length,
                            identity.Name,
                            identity.Version);
                        totalSourceArchiveBytes = checked(
                            totalSourceArchiveBytes + download.Archive.Length);
                        ValidateTotalSourceSize(totalSourceArchiveBytes);
                        byte[]? archive = download.Archive.ToArray();
                        try
                        {
                            var contentSha256 =
                                await ValidateCanonicalDigestAsync(
                                    download,
                                    archive,
                                    identity.Name,
                                    identity.Version,
                                    context.CancellationToken);
                            if (!string.Equals(
                                    contentSha256,
                                    identity.ContentSha256,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                throw new InvalidDataException(
                                    $"Fusion source '{identity.Name}@{identity.Version}' "
                                    + "changed between preflight and composition.");
                            }

                            sources.Add(
                                new FusionSessionSource(
                                    identity.Name,
                                    identity.Version,
                                    archive,
                                    contentSha256));
                            archive = null;
                        }
                        finally
                        {
                            if (archive is not null)
                            {
                                Array.Clear(archive);
                            }
                        }
                    }

                    context.CancellationToken.ThrowIfCancellationRequested();
                    state.SetSources(sources);
                    sourceListTransferred = true;
                }
                finally
                {
                    if (!sourceListTransferred)
                    {
                        foreach (var source in sources)
                        {
                            Array.Clear(source.Archive);
                        }
                    }
                }
            }
        }
        catch
        {
            session.Clear();
            throw;
        }
    }

    public async Task ComposeAsync(
        PipelineStepContext context,
        FusionPipelineSession session)
    {
        var deployments = session.GetDeployments();
        if (deployments.Count == 0)
        {
            return;
        }

        var workflow = context.Services
            .GetRequiredService<FusionDeploymentWorkflow>();
        var compositionResource = FusionPipeline.GetCompositionResource(
            context.Model);
        var currentComposition = GraphQLResourceModel.GetComposition(
            compositionResource);

        try
        {
            foreach (var deployment in deployments)
            {
                var state = session.GetState(deployment);
                await ValidateSessionStateAsync(
                    state,
                    deployment,
                    context,
                    context.CancellationToken);

                var compositionEnvironment = ResolveCompositionEnvironment(
                    deployment,
                    currentComposition.Settings);
                await using var farStream = new ClearingPooledMemoryStream(
                    _memoryLimits.FusionArchiveBytes,
                    "The composed Fusion archive exceeds the "
                    + $"{_memoryLimits.FusionArchiveBytes:N0}-byte in-memory size limit.");
                var logger = context.Services
                    .GetRequiredService<ILogger<SchemaComposition>>();
                var target = await ResolveTargetAsync(
                    deployment,
                    context,
                    context.CancellationToken);
                var stageSettings = await workflow.TryGetStageCompositionSettingsAsync(
                    target,
                    deployment.StageName,
                    logger,
                    context.CancellationToken);
                if (!await AspireCompositionHelper.TryComposeArchivesAsync(
                        farStream,
                        state.Sources.Select(
                                source => new SourceSchemaArchiveInfo(
                                    source.Name,
                                    source.Archive))
                            .ToArray(),
                        compositionEnvironment,
                        currentComposition.Settings,
                        stageSettings,
                        logger,
                        context.CancellationToken))
                {
                    throw new InvalidOperationException(
                        "Fusion configuration composition failed.");
                }

                TransferComposition(
                    state,
                    compositionEnvironment,
                    farStream.ToArray(),
                    context.CancellationToken);
            }
        }
        catch
        {
            session.Clear();
            throw;
        }
    }

    public async Task PublishAsync(
        PipelineStepContext context,
        FusionPipelineSession session)
    {
        var deployments = session.GetDeployments();
        if (deployments.Count == 0)
        {
            return;
        }

        var workflow = context.Services
            .GetRequiredService<FusionDeploymentWorkflow>();
        var compositionResource = FusionPipeline.GetCompositionResource(
            context.Model);
        var composition = GraphQLResourceModel.GetComposition(
            compositionResource);

        try
        {
            foreach (var deployment in deployments)
            {
                var state = session.GetState(deployment);
                await ValidateSessionStateAsync(
                    state,
                    deployment,
                    context,
                    context.CancellationToken);
                var target = await ResolveTargetAsync(
                    deployment,
                    context,
                    context.CancellationToken);
                ValidateCompositionState(
                    state,
                    deployment,
                    composition.Settings);
                VerifyMemoryDigest(
                    state.FusionArchive,
                    state.FusionArchiveSha256
                        ?? throw new InvalidOperationException(
                            "The composed Fusion archive has no digest."),
                    "composed Fusion archive");

                await workflow.PublishAsync(
                    new FusionPublicationRequest(
                        target,
                        deployment.StageName,
                        state.Tag,
                        state.SourceIdentities
                            .Select(source =>
                                new FusionSourceSchemaVersion(
                                    source.Name,
                                    source.Version))
                            .ToArray(),
                        deployment.WaitForApproval,
                        deployment.Force,
                        NitroStageResource.OperationTimeout,
                        NitroStageResource.ApprovalTimeout),
                    state.FusionArchive,
                    context.CancellationToken);
            }
        }
        finally
        {
            session.Clear();
        }
    }

    internal static async Task WaitForReadinessAsync(
        HttpClient httpClient,
        string sourceName,
        Uri endpoint,
        TimeSpan timeout,
        TimeSpan retryDelay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            timeout,
            TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(
            retryDelay,
            TimeSpan.Zero);

        using var timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
        timeoutSource.CancelAfter(timeout);
        Exception? lastFailure = null;

        while (!timeoutSource.IsCancellationRequested)
        {
            try
            {
                using var response = await httpClient.GetAsync(
                    endpoint,
                    timeoutSource.Token);
                if ((int)response.StatusCode
                    < (int)HttpStatusCode.InternalServerError)
                {
                    return;
                }

                lastFailure = new HttpRequestException(
                    $"The readiness endpoint returned HTTP {(int)response.StatusCode} "
                    + $"({response.StatusCode}).");
            }
            catch (HttpRequestException exception)
            {
                lastFailure = exception;
            }
            catch (OperationCanceledException exception)
                when (!cancellationToken.IsCancellationRequested)
            {
                lastFailure = exception;
            }

            if (timeoutSource.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await Task.Delay(
                    retryDelay,
                    timeoutSource.Token);
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        throw new InvalidOperationException(
            $"Fusion source '{sourceName}' at '{endpoint}' did not pass its "
            + $"production readiness check within {timeout}.",
            lastFailure);
    }

    internal async Task<IReadOnlyList<FusionSourceArtifact>> MaterializeArchivesAsync(
        PipelineStepContext context)
    {
        var apis = FusionPipeline.SelectApis(context.Model);

        if (apis.Count == 0)
        {
            return [];
        }

        var output = context.Services
            .GetRequiredService<IPipelineOutputService>()
            .GetOutputDirectory();
        var artifacts = new List<FusionSourceArtifact>();
        var compositionResource = FusionPipeline.GetCompositionResource(
            context.Model);
        var composition = GraphQLResourceModel.GetComposition(
            compositionResource);

        var invocation = ResolveInvocation(context);
        foreach (var api in apis)
        {
            var stages = FusionPipeline.GetStages(context.Model, api);
            var tag = invocation.Tag;
            var deploymentDirectory = GetApiDirectory(output, api);
            var materializedDirectory = IOPath.Combine(
                deploymentDirectory,
                "materialized");
            Directory.CreateDirectory(materializedDirectory);

            foreach (var sourceDirectory in Directory.EnumerateDirectories(
                    IOPath.Combine(deploymentDirectory, "sources"))
                .OrderBy(IOPath.GetFileName, StringComparer.Ordinal))
            {
                var name = IOPath.GetFileName(sourceDirectory);
                var sourceVersion = tag;
                ValidatePathSegment(sourceVersion, "source version");
                var schema = await File.ReadAllBytesAsync(
                    IOPath.Combine(sourceDirectory, "schema.graphqls"),
                    context.CancellationToken);
                var settingsPath = IOPath.Combine(
                    sourceDirectory,
                    "schema-settings.template.json");
                using var settings = JsonDocument.Parse(
                    await File.ReadAllTextAsync(
                        settingsPath,
                        context.CancellationToken));

                ValidateSettingsName(name, settings);

                // an immutable source version serves every stage of the api, so the endpoint of
                // every declared stage has to be publicly reachable.
                foreach (var stage in stages)
                {
                    using var resolvedSettings =
                        AspireCompositionHelper.ResolveSourceSchemaSettings(
                            settings,
                            ResolveCompositionEnvironment(stage, composition.Settings));
                    RejectLoopbackEndpoint(GetTransportEndpoint(resolvedSettings));
                }

                var archivePath = IOPath.Combine(
                    materializedDirectory,
                    $"{name}-{sourceVersion}.zip");
                await CreateArchiveAsync(
                    archivePath,
                    schema,
                    settings,
                    GetExtensionsPath(sourceDirectory),
                    context.CancellationToken);
                var digest = await ComputeFileDigestAsync(
                    archivePath,
                    context.CancellationToken);

                artifacts.Add(
                    new(
                        api,
                        name,
                        sourceVersion,
                        archivePath,
                        digest));
            }
        }

        return artifacts;
    }

    private async Task CreateDeploymentArtifactsAsync(
        NitroApiResource api,
        IReadOnlyList<NitroStageResource> stages,
        IReadOnlyList<GraphQLSourceSchemaResource> sources,
        string tag,
        string output,
        IServiceProvider services,
        ILogger<SchemaComposition> logger,
        CancellationToken cancellationToken)
    {
        var deploymentDirectory = GetApiDirectory(output, api);
        var fusionDirectory = IOPath.GetDirectoryName(deploymentDirectory)!;
        Directory.CreateDirectory(fusionDirectory);
        var temporaryDirectory = IOPath.Combine(
            fusionDirectory,
            $".{api.Name}.{Guid.NewGuid():N}.tmp");

        try
        {
            var sourcesDirectory = IOPath.Combine(temporaryDirectory, "sources");
            Directory.CreateDirectory(sourcesDirectory);
            var exportDirectory = IOPath.Combine(temporaryDirectory, "export");
            var sourceNames = new List<string>(sources.Count);

            foreach (var source in sources)
            {
                sourceNames.Add(
                    await CreateSourceArtifactsAsync(
                        source,
                        sourcesDirectory,
                        exportDirectory,
                        services,
                        logger,
                        cancellationToken));
            }

            // The export scratch space must not become part of the published output.
            if (Directory.Exists(exportDirectory))
            {
                Directory.Delete(exportDirectory, recursive: true);
            }

            var template = new FusionDeploymentTemplate(
                FormatVersion: 1,
                CloudUrl: api.Nitro.CloudUrl!,
                ApiId: api.ApiId!,
                Stages: stages
                    .Select(stage => stage.StageName)
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                ConfigurationTag: tag,
                StageOwnership: "authoritative",
                Sources: sourceNames.Order(StringComparer.Ordinal).ToArray());

            await WriteJsonAtomicallyAsync(
                IOPath.Combine(temporaryDirectory, "nitro-deployment-template.json"),
                template,
                cancellationToken);

            ReplaceDirectoryAtomically(
                temporaryDirectory,
                deploymentDirectory);
        }
        finally
        {
            DeleteDirectoryBestEffort(temporaryDirectory);
        }
    }

    internal static void ReplaceDirectoryAtomically(
        string sourceDirectory,
        string destinationDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Replacement directory '{sourceDirectory}' does not exist.");
        }

        var destinationParent = IOPath.GetDirectoryName(destinationDirectory)
            ?? throw new InvalidOperationException(
                $"Replacement destination '{destinationDirectory}' has no parent directory.");
        Directory.CreateDirectory(destinationParent);
        var backupDirectory = IOPath.Combine(
            destinationParent,
            $".{IOPath.GetFileName(destinationDirectory)}.{Guid.NewGuid():N}.bak");
        var movedDestination = false;

        try
        {
            if (Directory.Exists(destinationDirectory))
            {
                Directory.Move(destinationDirectory, backupDirectory);
                movedDestination = true;
            }

            try
            {
                Directory.Move(sourceDirectory, destinationDirectory);
            }
            catch (Exception moveException)
            {
                if (movedDestination && Directory.Exists(backupDirectory))
                {
                    // Once rollback starts, the backup must never be deleted by the finally block.
                    // If cleanup or restoration fails, preserving it is the only recoverable copy.
                    movedDestination = false;

                    try
                    {
                        if (Directory.Exists(destinationDirectory))
                        {
                            Directory.Delete(destinationDirectory, recursive: true);
                        }

                        Directory.Move(backupDirectory, destinationDirectory);
                    }
                    catch (Exception rollbackException)
                    {
                        throw new AggregateException(
                            "Replacing the deployment artifacts failed and the previous "
                            + "artifacts could not be restored. The backup remains at "
                            + $"'{backupDirectory}'.",
                            moveException,
                            rollbackException);
                    }
                }

                throw;
            }
        }
        finally
        {
            if (movedDestination)
            {
                DeleteDirectoryBestEffort(backupDirectory);
            }
        }
    }

    private async Task<string> CreateSourceArtifactsAsync(
        GraphQLSourceSchemaResource sourceSchema,
        string sourcesDirectory,
        string exportDirectory,
        IServiceProvider services,
        ILogger<SchemaComposition> logger,
        CancellationToken cancellationToken)
    {
        var source = sourceSchema.Resource;
        var declaration = sourceSchema.Declaration;
        var publishingSourceName = GetPublishingSourceName(sourceSchema);

        string schemaPath;
        string settingsPath;
        string? extensionsPath = null;
        string projectPath;
        string configuration;
        string? targetFramework;
        string? runtimeIdentifier;

        switch (declaration.Location)
        {
            case SourceSchemaLocationType.ProjectDirectory:
                var schemaPaths = GraphQLResourceModel.GetProjectSchemaPaths(
                    source,
                    declaration);
                projectPath = schemaPaths.ProjectPath;
                schemaPath = schemaPaths.SchemaPath;
                settingsPath = schemaPaths.SettingsPath;
                extensionsPath = schemaPaths.ExtensionsPath;
                configuration = "prebuilt";
                targetFramework = null;
                runtimeIdentifier = null;
                break;

            case SourceSchemaLocationType.CommandLineExport:
                var export = await GraphQLSchemaExportCommand.ExecuteAsync(
                    services,
                    source,
                    declaration,
                    IOPath.Combine(exportDirectory, source.Name),
                    cancellationToken);
                schemaPath = export.SchemaPath;
                settingsPath = export.SettingsPath;
                projectPath = export.ProjectPath;
                configuration = "project-default";
                targetFramework = null;
                runtimeIdentifier = null;
                break;

            case SourceSchemaLocationType.SchemaEndpoint:
                projectPath = GraphQLResourceModel.GetProjectPath(source);
                settingsPath = IOPath.Combine(
                    IOPath.GetDirectoryName(projectPath)!,
                    "schema-settings.json");

                if (!File.Exists(settingsPath))
                {
                    throw new InvalidOperationException(
                        $"GraphQL source '{source.Name}' did not provide schema-settings.json.");
                }

                using (var endpointSettings = JsonDocument.Parse(
                    await File.ReadAllTextAsync(
                        settingsPath,
                        cancellationToken)))
                {
                    var schemaEndpointConfiguration = SchemaComposition.ReadEndpointConfiguration(
                        source.Name,
                        publishingSourceName,
                        endpointSettings);
                    if (!SchemaComposition.TryGetSchemaFetchPath(
                        source.Name,
                        declaration,
                        schemaEndpointConfiguration,
                        logger,
                        out var schemaFetchPath))
                    {
                        throw new InvalidOperationException(
                            $"GraphQL source '{source.Name}' does not declare the path of its "
                            + "GraphQL endpoint. Call WithGraphQLHttpEndpoint on the resource.");
                    }

                    var schemaUrl = source.GetGraphQLSchemaUrl(
                        schemaFetchPath,
                        declaration.EndpointName);
                    if (schemaUrl is null)
                    {
                        throw new InvalidOperationException(
                            $"GraphQL source '{source.Name}' has no resolvable endpoint named "
                            + $"'{declaration.EndpointName}'. Endpoint-based publishing requires "
                            + "a fixed target port when the endpoint is not allocated.");
                    }

                    using var httpClient = _schemaHttpClientFactory();
                    var schemaText = await SchemaEndpointSchemaFetcher.FetchAsync(
                        schemaEndpointConfiguration.SourceSchemaName,
                        new Uri(schemaUrl),
                        schemaEndpointConfiguration.Protocol,
                        httpClient,
                        SchemaEndpointSchemaFetcher.DefaultMaxRetries,
                        SchemaEndpointSchemaFetcher.DefaultRetryDelay,
                        logger,
                        cancellationToken)
                        ?? throw new InvalidOperationException(
                            $"GraphQL source '{source.Name}' could not be downloaded from "
                            + $"'{schemaUrl}'.");
                    var endpointExportDirectory = IOPath.Combine(
                        exportDirectory,
                        source.Name);
                    Directory.CreateDirectory(endpointExportDirectory);
                    schemaPath = IOPath.Combine(
                        endpointExportDirectory,
                        "schema.graphqls");
                    await File.WriteAllTextAsync(
                        schemaPath,
                        schemaText,
                        cancellationToken);
                }

                configuration = "endpoint";
                targetFramework = null;
                runtimeIdentifier = null;
                break;

            default:
                throw new InvalidOperationException(
                    $"GraphQL source '{source.Name}' has an unsupported acquisition mode.");
        }

        if (!File.Exists(schemaPath) || !File.Exists(settingsPath))
        {
            throw new InvalidOperationException(
                $"GraphQL source '{source.Name}' did not provide both schema and settings files.");
        }

        var schema = await File.ReadAllTextAsync(schemaPath, cancellationToken);
        if (string.IsNullOrWhiteSpace(schema))
        {
            throw new InvalidOperationException(
                $"GraphQL source '{source.Name}' has an empty schema.");
        }

        string? extensions = null;
        if (extensionsPath is not null && File.Exists(extensionsPath))
        {
            extensions = await File.ReadAllTextAsync(
                extensionsPath,
                cancellationToken);
        }

        using var settings = JsonDocument.Parse(
            await File.ReadAllTextAsync(settingsPath, cancellationToken));
        var endpointConfiguration = SchemaComposition.ReadEndpointConfiguration(
            source.Name,
            publishingSourceName,
            settings);
        var name = endpointConfiguration.SourceSchemaName;
        ValidatePathSegment(name, "source schema name");
        GraphQLSourceSchemaValidator.Validate(
            source.Name,
            endpointConfiguration,
            schema,
            extensions);

        var sourceDirectory = IOPath.Combine(sourcesDirectory, name);
        Directory.CreateDirectory(sourceDirectory);
        var destinationSchema = IOPath.Combine(sourceDirectory, "schema.graphqls");
        var destinationSettings = IOPath.Combine(
            sourceDirectory,
            "schema-settings.template.json");
        await File.WriteAllTextAsync(
            destinationSchema,
            schema,
            cancellationToken);
        await File.WriteAllTextAsync(
            destinationSettings,
            settings.RootElement.GetRawText(),
            cancellationToken);

        if (extensions is not null)
        {
            await File.WriteAllTextAsync(
                IOPath.Combine(sourceDirectory, "schema-extensions.graphqls"),
                extensions,
                cancellationToken);
        }

        var projectDigest = await ComputeFileDigestAsync(
            projectPath,
            cancellationToken);
        var schemaDigest = await ComputeFileDigestAsync(
            destinationSchema,
            cancellationToken);
        var provenance = new FusionSourceProvenance(
            ProjectPath: projectPath,
            ProjectSha256: projectDigest,
            SchemaSha256: schemaDigest,
            Configuration: configuration,
            TargetFramework: targetFramework,
            RuntimeIdentifier: runtimeIdentifier,
            LaunchProfile: false,
            WorkingDirectory: IOPath.GetDirectoryName(projectPath)!);

        await WriteJsonAtomicallyAsync(
            IOPath.Combine(sourceDirectory, "provenance.json"),
            provenance,
            cancellationToken);

        return name;
    }

    private static async Task CreateArchiveAsync(
        string archivePath,
        byte[] schema,
        JsonDocument settings,
        string? extensionsPath,
        CancellationToken cancellationToken)
    {
        var temporaryPath = archivePath + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            using (var archive = FusionSourceSchemaArchive.Create(temporaryPath))
            {
                await archive.SetArchiveMetadataAsync(
                    new HotChocolate.Fusion.SourceSchema.Packaging.ArchiveMetadata(),
                    cancellationToken);
                await archive.SetSchemaAsync(schema, cancellationToken);
                await archive.SetSettingsAsync(settings, cancellationToken);

                if (extensionsPath is not null)
                {
                    await archive.SetSchemaExtensionsAsync(
                        await File.ReadAllBytesAsync(
                            extensionsPath,
                            cancellationToken),
                        cancellationToken);
                }

                await archive.CommitAsync(cancellationToken);
            }

            File.Move(temporaryPath, archivePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    internal static string ResolveCompositionEnvironment(
        NitroStageResource deployment,
        GraphQLCompositionSettings settings)
    {
        _ = settings;
        return deployment.StageName;
    }

    private static void ValidateCompositionState(
        FusionDeploymentSessionState state,
        NitroStageResource deployment,
        GraphQLCompositionSettings settings)
    {
        var expectedEnvironment = ResolveCompositionEnvironment(
            deployment,
            settings);
        if (!string.Equals(
                state.CompositionEnvironment,
                expectedEnvironment,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "The composed Fusion archive environment does not match "
                + $"deployment '{deployment.Name}'.");
        }
    }

    internal static JsonDocument ResolveSourceSchemaSettings(
        JsonDocument settings,
        string environmentName)
        => AspireCompositionHelper.ResolveSourceSchemaSettings(
            settings,
            environmentName);

    internal static IReadOnlyList<string> GetSourceNames(
        DistributedApplicationModel model)
    {
        var composition = FusionPipeline.GetCompositionResource(model);
        var sourceNames = GraphQLResourceModel
            .GetReferencedSourceSchemas(composition, model)
            .Select(GetPublishingSourceName)
            .ToArray();
        if (sourceNames.Length == 0)
        {
            throw new InvalidOperationException(
                $"Fusion composition resource '{composition.Name}' has no declared source schemas.");
        }

        var duplicateProvider = sourceNames
            .GroupBy(name => name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateProvider is not null)
        {
            throw new InvalidOperationException(
                "Multiple provider resources map to Fusion source "
                + $"'{duplicateProvider.Key}'.");
        }

        foreach (var sourceName in sourceNames)
        {
            ValidatePathSegment(sourceName, "source schema name");
        }

        Array.Sort(sourceNames, StringComparer.Ordinal);
        return sourceNames;
    }

    internal static string GetPublishingSourceName(
        GraphQLSourceSchemaResource source)
    {
        if (!string.IsNullOrWhiteSpace(source.Declaration.SourceSchemaName))
        {
            return source.Declaration.SourceSchemaName;
        }

        if (source.Declaration.Location is SourceSchemaLocationType.SchemaEndpoint)
        {
            throw new InvalidOperationException(
                $"GraphQL source '{source.Resource.Name}' must specify sourceSchemaName for "
                + "endpoint-based publishing. The deployment runner cannot derive it from "
                + "schema-settings.json.");
        }

        return source.Resource.Name;
    }

    private static async Task<FusionSourceSchemaDownload>
        DownloadExactSourceAsync(
            FusionDeploymentWorkflow workflow,
            FusionTarget target,
            NitroStageResource deployment,
            string sourceName,
            string tag,
            CancellationToken cancellationToken)
    {
        var download = await workflow.DownloadSourceSchemaAsync(
            target,
            new FusionSourceSchemaVersion(sourceName, tag),
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Fusion source '{sourceName}' version '{tag}' does not exist "
                + $"on API '{deployment.Api.ApiId}'.");
        if (!string.Equals(
                download.Name,
                sourceName,
                StringComparison.Ordinal)
            || !string.Equals(
                download.Version,
                tag,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Nitro returned Fusion source '{download.Name}@{download.Version}' "
                + $"when '{sourceName}@{tag}' was requested.");
        }

        return download;
    }

    private void ValidateSourceSize(
        int sourceBytes,
        string sourceName,
        string tag)
    {
        if (sourceBytes > _memoryLimits.SourceArchiveBytes)
        {
            throw new InvalidDataException(
                $"Downloaded Fusion source '{sourceName}@{tag}' exceeds "
                + $"the {_memoryLimits.SourceArchiveBytes:N0}-byte "
                + "per-source in-memory size limit.");
        }
    }

    private void ValidateTotalSourceSize(long totalSourceBytes)
    {
        if (totalSourceBytes > _memoryLimits.TotalSourceArchiveBytes)
        {
            throw new InvalidDataException(
                "Downloaded Fusion sources exceed the "
                + $"{_memoryLimits.TotalSourceArchiveBytes:N0}-byte aggregate in-memory size "
                + "limit across selected stages.");
        }
    }

    private static async Task<string> ValidateCanonicalDigestAsync(
        FusionSourceSchemaDownload download,
        byte[] archive,
        string sourceName,
        string tag,
        CancellationToken cancellationToken)
    {
        var contentSha256 =
            await FusionSourceSchemaContent.ComputeSha256Async(
                archive,
                sourceName,
                cancellationToken);
        if (!string.Equals(
                contentSha256,
                download.ContentSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Downloaded Fusion source '{sourceName}@{tag}' content "
                + "does not match its canonical digest.");
        }

        return contentSha256;
    }

    private static Task ValidatePreflightStateAsync(
        FusionDeploymentSessionState state,
        NitroStageResource deployment,
        PipelineStepContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tag = state.Tag;
        var sourceNames = GetSourceNames(context.Model);

        if (!string.Equals(
                state.CloudUrl.TrimEnd('/'),
                deployment.Api.Nitro.CloudUrl!.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                state.ApiId,
                deployment.Api.ApiId,
                StringComparison.Ordinal)
            || state.SourceIdentities.Count != sourceNames.Count
            || !state.SourceIdentities.Select(source => source.Name)
                .SequenceEqual(sourceNames, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Prepared Fusion state does not match deployment '{deployment.Name}'.");
        }

        foreach (var source in state.SourceIdentities)
        {
            if (!string.Equals(
                    source.Version,
                    tag,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Prepared Fusion source '{source.Name}' does not use tag '{tag}'.");
            }
        }

        return Task.CompletedTask;
    }

    private static async Task ValidateSessionStateAsync(
        FusionDeploymentSessionState state,
        NitroStageResource deployment,
        PipelineStepContext context,
        CancellationToken cancellationToken)
    {
        await ValidatePreflightStateAsync(
            state,
            deployment,
            context,
            cancellationToken);
        var sources = state.Sources;
        if (sources.Count != state.SourceIdentities.Count
            || !sources.Select(source => source.Name)
                .SequenceEqual(
                    state.SourceIdentities.Select(source => source.Name),
                    StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Materialized Fusion sources do not match deployment '{deployment.Name}'.");
        }

        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var identity = state.SourceIdentities[i];
            if (!string.Equals(
                    source.Version,
                    identity.Version,
                    StringComparison.Ordinal)
                || !string.Equals(
                    source.ContentSha256,
                    identity.ContentSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Materialized Fusion source '{source.Name}' does not match preflight.");
            }

            var contentSha256 =
                await FusionSourceSchemaContent.ComputeSha256Async(
                    source.Archive,
                    source.Name,
                    cancellationToken);
            if (!string.Equals(
                    contentSha256,
                    source.ContentSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    $"Prepared Fusion source '{source.Name}' content changed after download.");
            }
        }
    }

    internal static IReadOnlyList<(string Name, Uri Endpoint)>
        GetTransportEndpoints(JsonDocument gatewaySettings)
    {
        if (!gatewaySettings.RootElement.TryGetProperty(
                "sourceSchemas",
                out var sourceSchemas)
            || sourceSchemas.ValueKind is not JsonValueKind.Object)
        {
            throw new InvalidDataException(
                "The Fusion gateway settings contain no source schemas.");
        }

        var endpoints = new List<(string Name, Uri Endpoint)>();
        foreach (var sourceSchema in sourceSchemas.EnumerateObject())
        {
            if (!sourceSchema.Value.TryGetProperty(
                    "transports",
                    out var transports)
                || !transports.TryGetProperty("http", out var http)
                || !http.TryGetProperty("url", out var url)
                || url.ValueKind is not JsonValueKind.String
                || !Uri.TryCreate(
                    url.GetString(),
                    UriKind.Absolute,
                    out var endpoint))
            {
                throw new InvalidDataException(
                    $"Fusion gateway settings source '{sourceSchema.Name}' "
                    + "must specify an absolute transports.http.url.");
            }

            endpoints.Add((sourceSchema.Name, endpoint));
        }

        return endpoints;
    }

    private static Task<FusionTarget> ResolveTargetAsync(
        NitroStageResource deployment,
        PipelineStepContext context,
        CancellationToken cancellationToken)
        => ResolveTargetAsync(deployment.Api, context, cancellationToken);

    private static FusionPipelineInvocation ResolveInvocation(
        PipelineStepContext context)
        => FusionPipelineInvocation.Resolve(
            context.Services.GetRequiredService<IConfiguration>());

    private static async Task<FusionTarget> ResolveTargetAsync(
        NitroApiResource api,
        PipelineStepContext context,
        CancellationToken cancellationToken)
    {
        var cloudUrl = new Uri(api.Nitro.CloudUrl!, UriKind.Absolute);
        var timeProvider = context.Services.GetService<TimeProvider>()
            ?? TimeProvider.System;
        var connectionResolver = new NitroConnectionResolver(
            new NitroSessionManager(
                new NitroSessionReader(
                    NitroDefaults.GetSessionFilePath(),
                    NitroDefaults.SessionRereadDelay),
                new NitroTokenRefreshClient(s_tokenRefreshHttpClient),
                timeProvider,
                NitroDefaults.AccessTokenExpiryGrace),
            SystemNitroEnvironment.Instance,
            cloudUrl);

        return await ResolveTargetAsync(
            api,
            context.Services.GetRequiredService<IConfiguration>(),
            connectionResolver,
            context.Services.GetService<ILogger<FusionPipelineExecutor>>()
                ?? NullLogger<FusionPipelineExecutor>.Instance,
            cancellationToken);
    }

    internal static async Task<FusionTarget> ResolveTargetAsync(
        NitroApiResource api,
        IConfiguration configuration,
        NitroConnectionResolver connectionResolver,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(api);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(connectionResolver);
        ArgumentNullException.ThrowIfNull(logger);

        var apiKey = api.Nitro.ApiKey is null
            ? configuration["Nitro:ApiKey"]
                ?? configuration["NITRO_API_KEY"]
            : await api.Nitro.ApiKey.GetValueAsync(cancellationToken);
        NitroCredential credential;

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            credential = NitroCredential.FromApiKey(apiKey);
        }
        else
        {
            var connection = await connectionResolver.ResolveAsync(
                logger,
                cancellationToken);
            credential = connection.Credential;
        }

        if (credential.Kind is NitroCredentialKind.None)
        {
            throw new InvalidOperationException(
                $"Nitro API '{api.ApiName}' requires a credential. "
                + credential.UnavailableMessage);
        }

        return new(
            new Uri(api.Nitro.CloudUrl!, UriKind.Absolute),
            api.ApiId!,
            credential);
    }

    internal static Uri GetTransportEndpoint(JsonDocument settings)
    {
        var root = settings.RootElement;
        if (!root.TryGetProperty("transports", out var transports)
            || !transports.TryGetProperty("http", out var http)
            || !http.TryGetProperty("url", out var url)
            || url.ValueKind is not JsonValueKind.String
            || !Uri.TryCreate(url.GetString(), UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                "Fusion deployment settings must specify an absolute production "
                + "transports.http.url.");
        }

        return endpoint;
    }

    private static void RejectLoopbackEndpoint(Uri endpoint)
    {
        if (endpoint.IsLoopback
            || endpoint.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Fusion deployment settings must not contain a loopback production endpoint.");
        }
    }

    private static void ValidateSettingsName(
        string expectedName,
        JsonDocument settings)
    {
        if (!settings.RootElement.TryGetProperty("name", out var name)
            || name.ValueKind is not JsonValueKind.String
            || !string.Equals(
                name.GetString(),
                expectedName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Fusion source settings name must exactly match '{expectedName}'.");
        }
    }

    private static string? GetExtensionsPath(string sourceDirectory)
    {
        var path = IOPath.Combine(
            sourceDirectory,
            "schema-extensions.graphqls");
        return File.Exists(path) ? path : null;
    }

    internal static void ValidatePathSegment(
        string value,
        string description)
    {
        if (value is "." or ".."
            || value.Any(character =>
                !char.IsAsciiLetterOrDigit(character)
                && character is not '.' and not '_' and not '-'))
        {
            throw new InvalidOperationException(
                $"Fusion {description} '{value}' cannot be used as a portable path segment.");
        }
    }

    private static async Task<string> ComputeFileDigestAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var digest = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexStringLower(digest);
    }

    private static string ComputeMemoryDigest(byte[] content)
        => Convert.ToHexStringLower(SHA256.HashData(content));

    internal void TransferComposition(
        FusionDeploymentSessionState state,
        string compositionEnvironment,
        byte[] fusionArchive,
        CancellationToken cancellationToken)
    {
        var transferred = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.SetComposition(
                compositionEnvironment,
                fusionArchive,
                ComputeMemoryDigest(fusionArchive));
            transferred = true;
        }
        finally
        {
            if (!transferred)
            {
                Array.Clear(fusionArchive);
            }
        }
    }

    private static void VerifyMemoryDigest(
        byte[] content,
        string expectedSha256,
        string description)
    {
        var actualSha256 = ComputeMemoryDigest(content);
        if (!string.Equals(
                actualSha256,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"The {description} SHA-256 does not match the in-memory session state.");
        }
    }

    internal static async Task VerifyFileDigestAsync(
        string path,
        string expectedSha256,
        string description,
        CancellationToken cancellationToken)
    {
        var actualSha256 = await ComputeFileDigestAsync(
            path,
            cancellationToken);
        if (!string.Equals(
                actualSha256,
                expectedSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"The {description} SHA-256 does not match prepared state.");
        }
    }

    private static async Task WriteJsonAtomicallyAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(IOPath.GetDirectoryName(path)!);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    value,
                    s_jsonOptions,
                    cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string GetApiDirectory(
        string output,
        NitroApiResource api)
        => IOPath.Combine(output, "fusion", api.Name);

    private static void DeleteDirectoryBestEffort(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed record FusionSourceArtifact(
    NitroApiResource Target,
    string Name,
    string Version,
    string ArchivePath,
    string Sha256);

internal sealed record FusionDeploymentTemplate(
    int FormatVersion,
    string CloudUrl,
    string ApiId,
    IReadOnlyList<string> Stages,
    string ConfigurationTag,
    string StageOwnership,
    IReadOnlyList<string> Sources);

internal sealed record FusionSourceProvenance(
    string ProjectPath,
    string ProjectSha256,
    string SchemaSha256,
    string Configuration,
    string? TargetFramework,
    string? RuntimeIdentifier,
    bool LaunchProfile,
    string WorkingDirectory);

#pragma warning restore ASPIREPIPELINES004
#pragma warning restore ASPIREPIPELINES001
