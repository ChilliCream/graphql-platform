using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using ChilliCream.Nitro.Fusion;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Aspire;
using HotChocolate.Fusion.Packaging;
using HotChocolate.Fusion.SourceSchema.Packaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChilliCream.Nitro.Aspire;

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES004

internal sealed class FusionPipelineExecutor : IFusionPipelineExecutor
{
    private static readonly TimeSpan s_readinessRetryDelay =
        TimeSpan.FromSeconds(1);
    private static readonly JsonSerializerOptions s_jsonOptions = new(
        JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static FusionPipelineExecutor Instance { get; } = new();

    public async Task CreateArtifactsAsync(PipelineStepContext context)
    {
        var environment = context.Services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        var deployments = FusionPipeline.SelectDeployments(
            context.Model,
            environment);

        if (deployments.Count == 0)
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

        foreach (var deployment in deployments)
        {
            await CreateDeploymentArtifactsAsync(
                deployment,
                sources,
                output,
                context.CancellationToken);
        }
    }

    public async Task VerifyReadinessAsync(PipelineStepContext context)
    {
        var environment = context.Services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        var deployments = FusionPipeline.SelectDeployments(
            context.Model,
            environment);

        if (deployments.Count == 0)
        {
            return;
        }

        var compositionResource = FusionPipeline.GetCompositionResource(
            context.Model);
        var composition = GraphQLResourceModel.GetComposition(
            compositionResource);
        var output = context.Services
            .GetRequiredService<IPipelineOutputService>()
            .GetOutputDirectory();
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        foreach (var deployment in deployments)
        {
            var applyDirectory = GetApplyDirectory(output, deployment);
            var state = await ReadApplyStateAsync(
                applyDirectory,
                context.CancellationToken);
            await ValidateApplyStateAsync(
                state,
                deployment,
                context,
                context.CancellationToken);
            ValidateCompositionState(
                state,
                deployment,
                composition.Settings);
            var farPath = ResolveApplyPath(
                applyDirectory,
                state.FusionArchivePath
                    ?? throw new InvalidOperationException(
                        "The downloaded Fusion sources have not been composed."));
            await VerifyFileDigestAsync(
                farPath,
                state.FusionArchiveSha256
                    ?? throw new InvalidOperationException(
                        "The composed Fusion archive has no digest."),
                "composed Fusion archive",
                context.CancellationToken);
            using var archive = FusionArchive.Open(farPath);
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
                    deployment.OperationTimeout,
                    s_readinessRetryDelay,
                    context.CancellationToken);
            }
        }
    }

    public async Task UploadAsync(PipelineStepContext context)
    {
        var artifacts = await MaterializeArchivesAsync(context);
        if (artifacts.Count == 0)
        {
            return;
        }

        var workflow = context.Services.GetRequiredService<IFusionDeploymentWorkflow>();

        foreach (var group in artifacts.GroupBy(artifact => artifact.Deployment))
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

    public async Task DownloadAsync(PipelineStepContext context)
    {
        var environment = context.Services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        var deployments = FusionPipeline.SelectDeployments(
            context.Model,
            environment);
        if (deployments.Count == 0)
        {
            return;
        }

        var workflow = context.Services
            .GetRequiredService<IFusionDeploymentWorkflow>();
        var output = context.Services
            .GetRequiredService<IPipelineOutputService>()
            .GetOutputDirectory();
        var sourceNames = GetSourceNames(context.Model);

        foreach (var deployment in deployments)
        {
            var tag = await ResolveConfigurationTagAsync(
                deployment,
                context.CancellationToken);
            var target = await ResolveTargetAsync(
                deployment,
                context,
                context.CancellationToken);
            var applyDirectory = GetApplyDirectory(output, deployment);
            var applyParent = Path.GetDirectoryName(applyDirectory)!;
            Directory.CreateDirectory(applyParent);
            var temporaryDirectory = Path.Combine(
                applyParent,
                $".{deployment.Name}.{Guid.NewGuid():N}.tmp");

            try
            {
                var sources = new List<FusionApplySource>(sourceNames.Count);

                foreach (var sourceName in sourceNames)
                {
                    var download = await workflow.DownloadSourceSchemaAsync(
                        target,
                        new FusionSourceSchemaVersion(
                            sourceName,
                            tag),
                        context.CancellationToken)
                        ?? throw new InvalidOperationException(
                            $"Fusion source '{sourceName}' version "
                            + $"'{tag}' does not exist on target "
                            + $"'{deployment.Nitro.ApiId}'.");
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

                    var relativePath = Path.Combine(
                        "sources",
                        sourceName,
                        $"{tag}.zip");
                    var archivePath = Path.Combine(
                        temporaryDirectory,
                        relativePath);
                    Directory.CreateDirectory(
                        Path.GetDirectoryName(archivePath)!);
                    await File.WriteAllBytesAsync(
                        archivePath,
                        download.Archive.ToArray(),
                        context.CancellationToken);
                    var contentSha256 =
                        await FusionSourceSchemaContent.ComputeSha256Async(
                            archivePath,
                            sourceName,
                            context.CancellationToken);
                    if (!string.Equals(
                            contentSha256,
                            download.ContentSha256,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException(
                            $"Downloaded Fusion source '{sourceName}@{tag}' content "
                            + "does not match its canonical digest.");
                    }

                    sources.Add(
                        new FusionApplySource(
                            sourceName,
                            tag,
                            relativePath.Replace(
                                Path.DirectorySeparatorChar,
                                '/'),
                            download.ContentSha256));
                }

                await WriteJsonAtomicallyAsync(
                    Path.Combine(
                        temporaryDirectory,
                        "fusion-apply.json"),
                    new FusionApplyState(
                        tag,
                        deployment.Nitro.CloudUrl!,
                        deployment.Nitro.ApiId!,
                        CompositionEnvironment: null,
                        FusionArchivePath: null,
                        FusionArchiveSha256: null,
                        sources),
                    context.CancellationToken);

                ReplaceDirectoryAtomically(
                    temporaryDirectory,
                    applyDirectory);
            }
            finally
            {
                DeleteDirectoryBestEffort(temporaryDirectory);
            }
        }
    }

    public async Task ComposeAsync(PipelineStepContext context)
    {
        var environment = context.Services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        var deployments = FusionPipeline.SelectDeployments(
            context.Model,
            environment);
        if (deployments.Count == 0)
        {
            return;
        }

        var output = context.Services
            .GetRequiredService<IPipelineOutputService>()
            .GetOutputDirectory();
        var compositionResource = FusionPipeline.GetCompositionResource(
            context.Model);
        var currentComposition = GraphQLResourceModel.GetComposition(
            compositionResource);

        foreach (var deployment in deployments)
        {
            var applyDirectory = GetApplyDirectory(output, deployment);
            var state = await ReadApplyStateAsync(
                applyDirectory,
                context.CancellationToken);
            await ValidateApplyStateAsync(
                state,
                deployment,
                context,
                context.CancellationToken);

            var compositionEnvironment = ResolveCompositionEnvironment(
                deployment,
                currentComposition.Settings);
            var farPath = Path.Combine(
                applyDirectory,
                "fusion-configuration.far");

            foreach (var source in state.Sources)
            {
                var archivePath = ResolveApplyPath(
                    applyDirectory,
                    source.ArchivePath);
                var contentSha256 =
                    await FusionSourceSchemaContent.ComputeSha256Async(
                        archivePath,
                        source.Name,
                        context.CancellationToken);
                if (!string.Equals(
                        contentSha256,
                        source.ContentSha256,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        $"Prepared Fusion source '{source.Name}' content changed after download.");
                }
            }

            if (File.Exists(farPath))
            {
                File.Delete(farPath);
            }

            var logger = context.Services
                .GetRequiredService<ILogger<SchemaComposition>>();
            if (!await AspireCompositionHelper.TryComposeArchivesAsync(
                    farPath,
                    state.Sources.Select(
                            source => new SourceSchemaArchiveInfo(
                                source.Name,
                                ResolveApplyPath(
                                    applyDirectory,
                                    source.ArchivePath)))
                        .ToArray(),
                    compositionEnvironment,
                    currentComposition.Settings,
                    logger,
                    context.CancellationToken))
            {
                throw new InvalidOperationException(
                    "Fusion configuration composition failed.");
            }

            await WriteJsonAtomicallyAsync(
                Path.Combine(applyDirectory, "fusion-apply.json"),
                state with
                {
                    CompositionEnvironment = compositionEnvironment,
                    FusionArchivePath = Path.GetRelativePath(
                            applyDirectory,
                            farPath)
                        .Replace(Path.DirectorySeparatorChar, '/'),
                    FusionArchiveSha256 = await ComputeFileDigestAsync(
                        farPath,
                        context.CancellationToken)
                },
                context.CancellationToken);
        }
    }

    public async Task PublishAsync(PipelineStepContext context)
    {
        var environment = context.Services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        var deployments = FusionPipeline.SelectDeployments(
            context.Model,
            environment);
        if (deployments.Count == 0)
        {
            return;
        }

        var output = context.Services
            .GetRequiredService<IPipelineOutputService>()
            .GetOutputDirectory();
        var workflow = context.Services
            .GetRequiredService<IFusionDeploymentWorkflow>();
        var compositionResource = FusionPipeline.GetCompositionResource(
            context.Model);
        var composition = GraphQLResourceModel.GetComposition(
            compositionResource);

        foreach (var deployment in deployments)
        {
            var applyDirectory = GetApplyDirectory(output, deployment);
            var state = await ReadApplyStateAsync(
                applyDirectory,
                context.CancellationToken);
            await ValidateApplyStateAsync(
                state,
                deployment,
                context,
                context.CancellationToken);
            var target = await ResolveTargetAsync(
                deployment,
                context,
                context.CancellationToken);
            var farPath = ResolveApplyPath(
                applyDirectory,
                state.FusionArchivePath
                    ?? throw new InvalidOperationException(
                        "The downloaded Fusion sources have not been composed."));
            ValidateCompositionState(
                state,
                deployment,
                composition.Settings);

            await VerifyFileDigestAsync(
                farPath,
                state.FusionArchiveSha256
                    ?? throw new InvalidOperationException(
                        "The composed Fusion archive has no digest."),
                "composed Fusion archive",
                context.CancellationToken);

            await workflow.PublishAsync(
                new FusionPublicationRequest(
                    target,
                    deployment.StageName!,
                    state.Tag,
                    state.Sources
                        .Select(source =>
                            new FusionSourceSchemaVersion(
                                source.Name,
                                source.Version))
                        .ToArray(),
                    deployment.WaitForApproval,
                    deployment.Force,
                    deployment.OperationTimeout,
                    deployment.ApprovalTimeout),
                farPath,
                context.CancellationToken);
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
        var environment = context.Services
            .GetRequiredService<IHostEnvironment>()
            .EnvironmentName;
        var deployments = FusionPipeline.SelectDeployments(
            context.Model,
            environment);

        if (deployments.Count == 0)
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

        foreach (var deployment in deployments)
        {
            var compositionEnvironment = ResolveCompositionEnvironment(
                deployment,
                composition.Settings);
            var tag = await ResolveConfigurationTagAsync(
                deployment,
                context.CancellationToken);
            var deploymentDirectory = GetDeploymentDirectory(output, deployment);
            var materializedDirectory = Path.Combine(
                deploymentDirectory,
                "materialized");
            Directory.CreateDirectory(materializedDirectory);

            foreach (var sourceDirectory in Directory.EnumerateDirectories(
                    Path.Combine(deploymentDirectory, "sources"))
                .OrderBy(Path.GetFileName, StringComparer.Ordinal))
            {
                var name = Path.GetFileName(sourceDirectory);
                var sourceVersion = tag;
                ValidatePathSegment(sourceVersion, "source version");
                var schema = await File.ReadAllBytesAsync(
                    Path.Combine(sourceDirectory, "schema.graphqls"),
                    context.CancellationToken);
                var settingsPath = Path.Combine(
                    sourceDirectory,
                    "schema-settings.template.json");
                using var settings = JsonDocument.Parse(
                    await File.ReadAllTextAsync(
                        settingsPath,
                        context.CancellationToken));

                ValidateSettingsName(name, settings);

                using var resolvedSettings =
                    AspireCompositionHelper.ResolveSourceSchemaSettings(
                        settings,
                        compositionEnvironment);
                var endpoint = GetTransportEndpoint(resolvedSettings);
                RejectLoopbackEndpoint(endpoint);

                var archivePath = Path.Combine(
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
                        deployment,
                        name,
                        sourceVersion,
                        archivePath,
                        digest));
            }
        }

        return artifacts;
    }

    private static async Task CreateDeploymentArtifactsAsync(
        FusionDeploymentResource deployment,
        IReadOnlyList<GraphQLSourceSchemaResource> sources,
        string output,
        CancellationToken cancellationToken)
    {
        var deploymentDirectory = GetDeploymentDirectory(output, deployment);
        var fusionDirectory = Path.GetDirectoryName(deploymentDirectory)!;
        Directory.CreateDirectory(fusionDirectory);
        var temporaryDirectory = Path.Combine(
            fusionDirectory,
            $".{deployment.Name}.{Guid.NewGuid():N}.tmp");

        try
        {
            var sourcesDirectory = Path.Combine(temporaryDirectory, "sources");
            Directory.CreateDirectory(sourcesDirectory);
            var sourceNames = new List<string>(sources.Count);

            foreach (var source in sources)
            {
                sourceNames.Add(
                    await CreateSourceArtifactsAsync(
                        source,
                        sourcesDirectory,
                        cancellationToken));
            }

            var template = new FusionDeploymentTemplate(
                FormatVersion: 1,
                CloudUrl: deployment.Nitro.CloudUrl!,
                ApiId: deployment.Nitro.ApiId!,
                Environment: deployment.EnvironmentName!,
                Stage: deployment.StageName!,
                ConfigurationTag: deployment.ConfigurationTag
                    ?? $"{{{{{deployment.ConfigurationTagParameter!.Name}}}}}",
                StageOwnership: "authoritative",
                Sources: sourceNames.Order(StringComparer.Ordinal).ToArray());

            await WriteJsonAtomicallyAsync(
                Path.Combine(temporaryDirectory, "nitro-deployment-template.json"),
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

        var destinationParent = Path.GetDirectoryName(destinationDirectory)
            ?? throw new InvalidOperationException(
                $"Replacement destination '{destinationDirectory}' has no parent directory.");
        Directory.CreateDirectory(destinationParent);
        var backupDirectory = Path.Combine(
            destinationParent,
            $".{Path.GetFileName(destinationDirectory)}.{Guid.NewGuid():N}.bak");
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
            catch
            {
                if (movedDestination
                    && !Directory.Exists(destinationDirectory)
                    && Directory.Exists(backupDirectory))
                {
                    Directory.Move(backupDirectory, destinationDirectory);
                    movedDestination = false;
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

    private static async Task<string> CreateSourceArtifactsAsync(
        GraphQLSourceSchemaResource sourceSchema,
        string sourcesDirectory,
        CancellationToken cancellationToken)
    {
        var source = sourceSchema.Resource;
        var declaration = sourceSchema.Declaration;

        string schemaPath;
        string settingsPath;
        string? extensionsPath = null;
        string projectPath;
        string configuration;
        string? targetFramework;
        string? runtimeIdentifier;
        using var temporaryExportDirectory = new TemporaryDirectoryScope();

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
                var exportDirectory =
                    temporaryExportDirectory.Create(source.Name);
                var export = await CommandLineSchemaExporter.ExportAsync(
                    source,
                    declaration,
                    exportDirectory,
                    cancellationToken);
                schemaPath = export.SchemaPath;
                settingsPath = export.SettingsPath;
                projectPath = export.ProjectPath;
                configuration = export.Configuration;
                targetFramework = export.TargetFramework;
                runtimeIdentifier = export.RuntimeIdentifier;
                break;

            case SourceSchemaLocationType.SchemaEndpoint:
                throw new InvalidOperationException(
                    $"GraphQL source '{source.Name}' uses runtime endpoint acquisition, which "
                    + "is unavailable during Aspire publish. Declare a schema file or an "
                    + "explicit command-line export.");

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
            declaration.SourceSchemaName ?? source.Name,
            settings);
        var name = endpointConfiguration.SourceSchemaName;
        ValidatePathSegment(name, "source schema name");
        GraphQLSourceSchemaValidator.Validate(
            source.Name,
            endpointConfiguration,
            schema,
            extensions);

        var sourceDirectory = Path.Combine(sourcesDirectory, name);
        Directory.CreateDirectory(sourceDirectory);
        var destinationSchema = Path.Combine(sourceDirectory, "schema.graphqls");
        var destinationSettings = Path.Combine(
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
                Path.Combine(sourceDirectory, "schema-extensions.graphqls"),
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
            WorkingDirectory: Path.GetDirectoryName(projectPath)!);

        await WriteJsonAtomicallyAsync(
            Path.Combine(sourceDirectory, "provenance.json"),
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
        FusionDeploymentResource deployment,
        GraphQLCompositionSettings settings)
        => deployment.CompositionEnvironmentName
            ?? settings.EnvironmentName
            ?? deployment.StageName
            ?? throw new InvalidOperationException(
                $"Fusion deployment '{deployment.Name}' has no composition environment.");

    private static void ValidateCompositionState(
        FusionApplyState state,
        FusionDeploymentResource deployment,
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
            .Select(source =>
                source.Declaration.SourceSchemaName
                ?? source.Resource.Name)
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

    private static async Task ValidateApplyStateAsync(
        FusionApplyState state,
        FusionDeploymentResource deployment,
        PipelineStepContext context,
        CancellationToken cancellationToken)
    {
        var tag = await ResolveConfigurationTagAsync(
            deployment,
            cancellationToken);
        var sourceNames = GetSourceNames(context.Model);
        if (!string.Equals(
                state.Tag,
                tag,
                StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Prepared Fusion tag '{state.Tag}' does not match configured tag '{tag}'.");
        }

        if (!string.Equals(
                state.CloudUrl.TrimEnd('/'),
                deployment.Nitro.CloudUrl!.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                state.ApiId,
                deployment.Nitro.ApiId,
                StringComparison.Ordinal)
            || state.Sources.Count != sourceNames.Count
            || !state.Sources.Select(source => source.Name)
                .SequenceEqual(sourceNames, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Prepared Fusion state does not match deployment '{deployment.Name}'.");
        }

        var applyDirectory = GetApplyDirectory(
            context.Services
                .GetRequiredService<IPipelineOutputService>()
                .GetOutputDirectory(),
            deployment);
        foreach (var source in state.Sources)
        {
            if (!string.Equals(
                    source.Version,
                    tag,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Prepared Fusion source '{source.Name}' does not use tag '{tag}'.");
            }

            var archivePath = ResolveApplyPath(
                applyDirectory,
                source.ArchivePath);
            var contentSha256 =
                await FusionSourceSchemaContent.ComputeSha256Async(
                    archivePath,
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

    private static async Task<FusionApplyState> ReadApplyStateAsync(
        string applyDirectory,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(
            applyDirectory,
            "fusion-apply.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "Fusion sources have not been downloaded.",
                path);
        }

        await using var stream = File.OpenRead(path);
        var state = await JsonSerializer.DeserializeAsync<FusionApplyState>(
                stream,
                s_jsonOptions,
                cancellationToken)
            ?? throw new InvalidDataException(
                "The Fusion apply state is empty.");
        if (string.IsNullOrWhiteSpace(state.Tag)
            || string.IsNullOrWhiteSpace(state.CloudUrl)
            || string.IsNullOrWhiteSpace(state.ApiId)
            || state.Sources is null
            || state.Sources.Any(source =>
                string.IsNullOrWhiteSpace(source.Name)
                || string.IsNullOrWhiteSpace(source.Version)
                || string.IsNullOrWhiteSpace(source.ArchivePath)
                || string.IsNullOrWhiteSpace(source.ContentSha256)))
        {
            throw new InvalidDataException(
                "The Fusion apply state is missing required content.");
        }

        return state;
    }

    private static string ResolveApplyPath(
        string applyDirectory,
        string relativePath)
    {
        if (Path.IsPathFullyQualified(relativePath))
        {
            throw new InvalidDataException(
                "A Fusion apply path must be relative.");
        }

        var fullApplyDirectory = Path.GetFullPath(applyDirectory);
        var path = Path.GetFullPath(relativePath, fullApplyDirectory);
        var prefix = fullApplyDirectory.EndsWith(
            Path.DirectorySeparatorChar)
            ? fullApplyDirectory
            : fullApplyDirectory + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "A Fusion apply path escapes the apply output directory.");
        }

        return path;
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

    private static async Task<FusionTarget> ResolveTargetAsync(
        FusionDeploymentResource deployment,
        PipelineStepContext context,
        CancellationToken cancellationToken)
    {
        var apiKey = deployment.Nitro.ApiKey is null
            ? context.Services.GetRequiredService<IConfiguration>()["Nitro:ApiKey"]
                ?? context.Services.GetRequiredService<IConfiguration>()["NITRO_API_KEY"]
            : await deployment.Nitro.ApiKey.GetValueAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"Nitro target '{deployment.Nitro.Name}' requires an API key.");
        }

        return new(
            new Uri(deployment.Nitro.CloudUrl!, UriKind.Absolute),
            deployment.Nitro.ApiId!,
            apiKey);
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
        var path = Path.Combine(
            sourceDirectory,
            "schema-extensions.graphqls");
        return File.Exists(path) ? path : null;
    }

    private static async Task<string> ResolveConfigurationTagAsync(
        FusionDeploymentResource deployment,
        CancellationToken cancellationToken)
    {
        var value = deployment.ConfigurationTag;
        if (deployment.ConfigurationTagParameter is not null)
        {
            value = await deployment.ConfigurationTagParameter.GetValueAsync(
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Fusion deployment '{deployment.Name}' configuration tag resolved to an empty value.");
        }

        ValidatePathSegment(value, "configuration tag");
        return value;
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
                $"The {description} SHA-256 does not match prepared apply state.");
        }
    }

    private static async Task WriteJsonAtomicallyAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
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

    private static string GetDeploymentDirectory(
        string output,
        FusionDeploymentResource deployment)
        => Path.Combine(output, "fusion", deployment.Name);

    private static string GetApplyDirectory(
        string output,
        FusionDeploymentResource deployment)
        => Path.Combine(output, "fusion", "apply", deployment.Name);

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

    private sealed class TemporaryDirectoryScope : IDisposable
    {
        private string? _path;

        public string Create(string resourceName)
        {
            _path = Path.Combine(
                Path.GetTempPath(),
                "chilicream-nitro-aspire",
                Guid.NewGuid().ToString("N"),
                resourceName);
            return _path;
        }

        public void Dispose()
        {
            try
            {
                if (_path is not null && Directory.Exists(_path))
                {
                    Directory.Delete(_path, recursive: true);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}

internal sealed record FusionSourceArtifact(
    FusionDeploymentResource Deployment,
    string Name,
    string Version,
    string ArchivePath,
    string Sha256);

internal sealed record FusionDeploymentTemplate(
    int FormatVersion,
    string CloudUrl,
    string ApiId,
    string Environment,
    string Stage,
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

internal sealed record FusionApplyState(
    string Tag,
    string CloudUrl,
    string ApiId,
    string? CompositionEnvironment,
    string? FusionArchivePath,
    string? FusionArchiveSha256,
    IReadOnlyList<FusionApplySource> Sources);

internal sealed record FusionApplySource(
    string Name,
    string Version,
    string ArchivePath,
    string ContentSha256);

#pragma warning restore ASPIREPIPELINES004
#pragma warning restore ASPIREPIPELINES001
