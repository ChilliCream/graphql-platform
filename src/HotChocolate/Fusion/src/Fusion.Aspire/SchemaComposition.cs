using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                var archivePath = Path.Combine(Path.GetDirectoryName(gatewayDirectory)!, settings.OutputFileName);
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
        catch (OperationCanceledException)
        {
            // we do nothing when we are cancelled.
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

            logger.LogInformation("Discovered source schema: {Name} -> {SchemaSource}",
                schemaInfo.Name,
                schemaInfo.HttpEndpointUrl?.ToString() ?? $"file://{schemaInfo.Schema.Name}");
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
                return await GetSourceSchemaFromEndpointAsync(resource, cancellationToken);

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
        CancellationToken cancellationToken)
    {
        var sourceSchemaName = resource.GetGraphQLSourceSchemaName() ?? resource.Name;

        var schemaUrl = resource.GetGraphQLSchemaUrl();
        if (schemaUrl == null)
        {
            logger.LogWarning("Could not determine schema URL for {ResourceName}", resource.Name);
            return null;
        }

        // Wait for the service to be ready and fetch schema
        var schemaText = await FetchSchemaFromEndpointAsync(schemaUrl, cancellationToken);
        if (schemaText == null)
        {
            return null;
        }

        // For endpoint schemas, look for "schema-settings.json" in the project directory
        var schemaSettings = await GetSourceSchemaSettingsAsync(resource, "schema-settings.json", cancellationToken);
        if (schemaSettings == null)
        {
            logger.LogWarning("Could not find schema-settings.json for {ResourceName}", resource.Name);
            return null;
        }

        return new SourceSchemaInfo
        {
            Name = sourceSchemaName,
            ResourceName = resource.Name,
            HttpEndpointUrl = new Uri(schemaUrl),
            Schema = new SourceSchemaText(sourceSchemaName, schemaText),
            SchemaSettings = schemaSettings
        };
    }

    private async Task<SourceSchemaInfo?> GetSourceSchemaFromFileAsync(
        IResourceWithEndpoints resource,
        GraphQLSourceSchemaAnnotation annotation,
        CancellationToken cancellationToken)
    {
        var sourceSchemaName = resource.GetGraphQLSourceSchemaName() ?? resource.Name;

        var schemaFromFile = await ReadSchemaFromProjectDirectoryAsync(resource, annotation.SchemaPath, cancellationToken);
        if (schemaFromFile == null)
        {
            return null;
        }

        // For file schemas, settings file is named after the schema file
        // e.g., "foo.graphql" -> "foo-settings.json"
        var schemaFileName = annotation.SchemaPath ?? "schema.graphql";
        var settingsFileName = $"{Path.GetFileNameWithoutExtension(schemaFileName)}-settings.json";

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
            Schema = new SourceSchemaText(sourceSchemaName, schemaFromFile),
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

            var projectDirectory = Path.GetDirectoryName(projectPath);
            var settingsFile = Path.Combine(projectDirectory!, settingsFileName);

            if (!File.Exists(settingsFile))
            {
                logger.LogWarning("Schema settings file not found: {SettingsFile}", settingsFile);
                return null;
            }

            var settingsJson = await File.ReadAllTextAsync(settingsFile, cancellationToken);
            return JsonDocument.Parse(settingsJson);
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

    private async Task<string?> FetchSchemaFromEndpointAsync(string schemaUrl, CancellationToken cancellationToken)
    {
        try
        {
            // Wait for the service to be ready
            if (!await WaitForServiceReadyAsync(schemaUrl, cancellationToken))
            {
                logger.LogWarning("Service not ready at {SchemaUrl}", schemaUrl);
                return null;
            }

            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var response = await httpClient.GetAsync(schemaUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch schema from {SchemaUrl}", schemaUrl);
            return null;
        }
    }

    private async Task<string?> ReadSchemaFromProjectDirectoryAsync(
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

            var projectDirectory = Path.GetDirectoryName(projectPath);
            var schemaFile = Path.Combine(projectDirectory!, fileName ?? "schema.graphql");

            if (!File.Exists(schemaFile))
            {
                logger.LogWarning("Schema file not found: {SchemaFile}", schemaFile);
                return null;
            }

            return await File.ReadAllTextAsync(schemaFile, cancellationToken);
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

    private async Task<bool> WaitForServiceReadyAsync(string url, CancellationToken cancellationToken)
    {
        const int maxRetries = 60;
        const int delayMs = 2000;

        logger.LogDebug("Waiting for service to be ready at {Url}", url);

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var response = await httpClient.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    // For GraphQL schema endpoints, also verify the content is valid
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        logger.LogDebug("Service ready at {Url}", url);
                        return true;
                    }
                }

                logger.LogDebug(
                    "Service not ready yet at {Url} (attempt {Attempt}/{MaxRetries})",
                    url,
                    i + 1,
                    maxRetries);
            }
            catch (Exception ex)
            {
                logger.LogDebug(
                    "Service check failed at {Url}: {Error} (attempt {Attempt}/{MaxRetries})",
                    url,
                    ex.Message,
                    i + 1,
                    maxRetries);
            }

            await Task.Delay(delayMs, cancellationToken);
        }

        logger.LogWarning(
            "Service failed to become ready at {Url} after {MaxRetries} attempts",
            url,
            maxRetries);
        return false;
    }

    private async Task<bool> ComposeSchemaAsync(
        string archivePath,
        List<SourceSchemaInfo> sourceSchemas,
        GraphQLSchemaCompositionAnnotation settings,
        CancellationToken cancellationToken)
    {
        var tempArchivePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

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
}
