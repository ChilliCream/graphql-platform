using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using ChilliCream.Nitro.Fusion;
using HotChocolate.Fusion.Aspire;
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

        var output = context.Services
            .GetRequiredService<IPipelineOutputService>()
            .GetOutputDirectory();
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        foreach (var deployment in deployments)
        {
            var deploymentDirectory = GetDeploymentDirectory(output, deployment);

            foreach (var sourceDirectory in Directory.EnumerateDirectories(
                Path.Combine(deploymentDirectory, "sources")))
            {
                var settingsPath = Path.Combine(
                    sourceDirectory,
                    "schema-settings.template.json");
                using var settings = JsonDocument.Parse(
                    await File.ReadAllTextAsync(
                        settingsPath,
                        context.CancellationToken));
                var endpoint = GetTransportEndpoint(settings);
                RejectLoopbackEndpoint(endpoint);

                using var response = await httpClient.GetAsync(
                    endpoint,
                    context.CancellationToken);
                if ((int)response.StatusCode >= (int)HttpStatusCode.InternalServerError)
                {
                    throw new InvalidOperationException(
                        $"Fusion source '{Path.GetFileName(sourceDirectory)}' did not pass "
                        + "its production readiness check.");
                }
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

    public async Task PublishAsync(PipelineStepContext context)
    {
        var artifacts = await MaterializeArchivesAsync(context);
        if (artifacts.Count == 0)
        {
            return;
        }

        var workflow = context.Services.GetRequiredService<IFusionDeploymentWorkflow>();
        var compositionResource = FusionPipeline.GetCompositionResource(context.Model);
        var composition = GraphQLResourceModel.GetComposition(compositionResource);

        foreach (var group in artifacts.GroupBy(artifact => artifact.Deployment))
        {
            var deployment = group.Key;
            var target = await ResolveTargetAsync(
                deployment,
                context,
                context.CancellationToken);
            var releaseId = await ResolveConfigurationTagAsync(
                deployment,
                context.CancellationToken);
            var farPath = Path.Combine(
                Path.GetDirectoryName(group.First().ArchivePath)!,
                $"{releaseId}.far");

            await ComposeAsync(
                farPath,
                group.ToArray(),
                composition.Settings,
                context,
                context.CancellationToken);

            await workflow.PublishAsync(
                new FusionPublicationRequest(
                    target,
                    deployment.StageName!,
                    releaseId,
                    group
                        .Select(artifact =>
                            new FusionSourceSchemaVersion(
                                artifact.Name,
                                artifact.Version))
                        .ToArray(),
                    deployment.WaitForApproval,
                    deployment.Force,
                    deployment.OperationTimeout,
                    deployment.ApprovalTimeout),
                farPath,
                context.CancellationToken);

            await WriteDeploymentManifestAsync(
                deployment,
                releaseId,
                group.ToArray(),
                context,
                context.CancellationToken);
        }
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

        foreach (var deployment in deployments)
        {
            var releaseId = await ResolveConfigurationTagAsync(
                deployment,
                context.CancellationToken);
            var deploymentDirectory = GetDeploymentDirectory(output, deployment);
            var materializedDirectory = Path.Combine(
                deploymentDirectory,
                "materialized");
            Directory.CreateDirectory(materializedDirectory);

            foreach (var sourceDirectory in Directory.EnumerateDirectories(
                Path.Combine(deploymentDirectory, "sources")))
            {
                var name = Path.GetFileName(sourceDirectory);
                var sourceVersion = deployment.UseGitCommitAsSourceVersion
                    ? await ReadGitCommitAsync(
                        sourceDirectory,
                        context.CancellationToken)
                    : releaseId;
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

                var endpoint = GetTransportEndpoint(settings);
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
                Sources: sourceNames.Order().ToArray());

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

    private static async Task ComposeAsync(
        string farPath,
        IReadOnlyList<FusionSourceArtifact> artifacts,
        GraphQLCompositionSettings settings,
        PipelineStepContext context,
        CancellationToken cancellationToken)
    {
        if (File.Exists(farPath))
        {
            File.Delete(farPath);
        }

        var logger = context.Services
            .GetRequiredService<ILogger<SchemaComposition>>();
        if (!await AspireCompositionHelper.TryComposeArchivesAsync(
                farPath,
                artifacts.Select(
                        artifact => new SourceSchemaArchiveInfo(
                            artifact.Name,
                            artifact.ArchivePath))
                    .ToArray(),
                settings,
                logger,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Fusion configuration composition failed.");
        }
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

    private static async Task WriteDeploymentManifestAsync(
        FusionDeploymentResource deployment,
        string releaseId,
        IReadOnlyList<FusionSourceArtifact> artifacts,
        PipelineStepContext context,
        CancellationToken cancellationToken)
    {
        var output = context.Services
            .GetRequiredService<IPipelineOutputService>()
            .GetOutputDirectory();
        var manifest = new FusionDeploymentManifest(
            FormatVersion: 1,
            CloudUrl: deployment.Nitro.CloudUrl!,
            ApiId: deployment.Nitro.ApiId!,
            Environment: deployment.EnvironmentName!,
            Stage: deployment.StageName!,
            ConfigurationTag: releaseId,
            StageOwnership: "authoritative",
            Sources: artifacts.Select(
                    artifact => new FusionDeploymentManifestSource(
                        artifact.Name,
                        artifact.Version,
                        Path.GetRelativePath(
                            GetDeploymentDirectory(output, deployment),
                            artifact.ArchivePath),
                        artifact.Sha256))
                .ToArray());

        await WriteJsonAtomicallyAsync(
            Path.Combine(
                GetDeploymentDirectory(output, deployment),
                "nitro-deployment.json"),
            manifest,
            cancellationToken);
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

    private static async Task<string> ReadGitCommitAsync(
        string sourceDirectory,
        CancellationToken cancellationToken)
    {
        using var provenance = JsonDocument.Parse(
            await File.ReadAllTextAsync(
                Path.Combine(sourceDirectory, "provenance.json"),
                cancellationToken));
        var workingDirectory = provenance.RootElement
            .GetProperty("workingDirectory")
            .GetString()!;
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(workingDirectory);
        startInfo.ArgumentList.Add("rev-parse");
        startInfo.ArgumentList.Add("HEAD");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start Git.");
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException(
                "Could not resolve the Git commit for a Fusion source version.");
        }

        return output.Trim();
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
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true
                    },
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

internal sealed record FusionDeploymentManifest(
    int FormatVersion,
    string CloudUrl,
    string ApiId,
    string Environment,
    string Stage,
    string ConfigurationTag,
    string StageOwnership,
    IReadOnlyList<FusionDeploymentManifestSource> Sources);

internal sealed record FusionDeploymentManifestSource(
    string Name,
    string SourceVersion,
    string Archive,
    string Sha256);

#pragma warning restore ASPIREPIPELINES004
#pragma warning restore ASPIREPIPELINES001
