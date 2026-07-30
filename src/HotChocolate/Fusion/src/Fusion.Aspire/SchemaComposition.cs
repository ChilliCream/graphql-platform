using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

internal sealed class SchemaComposition(
    IHostApplicationLifetime lifetime,
    ILogger<SchemaComposition> logger)
    : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(
        IDistributedApplicationEventing eventing,
        DistributedApplicationExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        eventing.Subscribe<AfterResourcesCreatedEvent>(async (@event, ct) =>
        {
            var model = @event.Services.GetRequiredService<DistributedApplicationModel>();
            var compositionFailed = false;

            try
            {
                // Find all resources that need schema composition
                var compositionResources = model.GetGraphQLCompositionResources().ToList();

                if (compositionResources.Count == 0)
                {
                    logger.LogDebug("No resources found that need GraphQL schema composition");
                    return;
                }

                logger.LogInformation("Starting GraphQL schema composition...");

                // Process each composition resource
                foreach (var compositionResource in compositionResources)
                {
                    if (!await ComposeSchemaAsync(compositionResource, model, ct))
                    {
                        compositionFailed = true;
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                compositionFailed = true;
            }

            if (compositionFailed)
            {
                logger.LogCritical("GraphQL schema composition failed - stopping application");
                lifetime.StopApplication();
                throw new InvalidOperationException("GraphQL schema composition failed");
            }
        });

        return Task.CompletedTask;
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

        logger.LogInformation(
            "Preparing schema composition for {ResourceName}.",
            compositionResource.Name);

        try
        {
            var sourceSchemas = await DiscoverReferencedSourceSchemasAsync(compositionResource, appModel, cancellationToken);

            if (sourceSchemas.Count == 0)
            {
                logger.LogWarning(
                    "{ResourceName} has no source schemas.",
                    compositionResource.Name);
                return true;
            }

            try
            {
                var gatewayDirectory = GetProjectPath(compositionResource)!;
                var archivePath = IOPath.Combine(IOPath.GetDirectoryName(gatewayDirectory)!, settings.OutputFileName);
                return await ComposeSchemaAsync(archivePath, sourceSchemas, settings, cancellationToken);
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
            logger.LogError(
                ex,
                "❌ Schema composition failed for {ResourceName}: {Error}",
                compositionResource.Name,
                ex.Message);
        }

        return false;
    }

    private async Task<List<SourceSchemaInfo>> DiscoverReferencedSourceSchemasAsync(
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
                    continue;
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

    private async Task<SourceSchemaInfo?> GetSourceSchemaInfoAsync(
        IResourceWithEndpoints resource,
        GraphQLSourceSchemaAnnotation sourceSchemaSettings,
        CancellationToken cancellationToken)
    {
        switch (sourceSchemaSettings.Location)
        {
            case SourceSchemaLocationType.SchemaEndpoint:
                return await GetSourceSchemaFromEndpointAsync(
                    resource,
                    sourceSchemaSettings,
                    cancellationToken);

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
                HttpEndpointUrl = null,
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
        const int maxRetries = 60;
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
            maxRetries,
            TimeSpan.FromSeconds(2),
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

    private async Task<bool> ComposeSchemaAsync(
        string archivePath,
        List<SourceSchemaInfo> sourceSchemas,
        GraphQLSchemaCompositionAnnotation settings,
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
                logger,
                cancellationToken))
            {
                File.Copy(tempArchivePath, archivePath, true);
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

    private static bool IsExtensionsSchemaPath(string filePath)
        => IOPath.GetFileNameWithoutExtension(filePath).EndsWith(
            "-extensions",
            StringComparison.OrdinalIgnoreCase);
}
