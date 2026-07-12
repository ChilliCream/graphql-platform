using System.Diagnostics.CodeAnalysis;
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

    [SuppressMessage(
        "Trimming",
        "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' "
        + "in call to target method. The return value of the source method does not have matching annotations.")]
    [SuppressMessage("ReSharper", "UnusedVariable")]
    private List<IResourceWithEndpoints> GetReferencedResources(
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
                return await GetSourceSchemaFromEndpointAsync(
                    resource,
                    sourceSchemaSettings,
                    cancellationToken);

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

        if (retryDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay));
        }

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
