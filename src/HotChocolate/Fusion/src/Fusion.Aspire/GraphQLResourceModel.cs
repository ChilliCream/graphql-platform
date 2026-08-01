using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

internal static class GraphQLResourceModel
{
    [SuppressMessage(
        "Trimming",
        "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' "
        + "in call to target method.")]
    public static IReadOnlyList<GraphQLSourceSchemaResource> GetReferencedSourceSchemas(
        IResourceWithEndpoints compositionResource,
        DistributedApplicationModel model)
    {
        ArgumentNullException.ThrowIfNull(compositionResource);
        ArgumentNullException.ThrowIfNull(model);

        var referencedResources = new HashSet<IResource>(
            ReferenceEqualityComparer.Instance);

        foreach (var annotation in compositionResource.Annotations)
        {
            switch (annotation)
            {
                case ResourceRelationshipAnnotation relationship:
                    referencedResources.Add(relationship.Resource);
                    break;

                case var endpointReference
                    when endpointReference.GetType().FullName
                        == "Aspire.Hosting.ApplicationModel.EndpointReferenceAnnotation":
                    var resourceProperty = endpointReference.GetType().GetProperty("Resource");
                    if (resourceProperty?.GetValue(endpointReference) is IResource resource)
                    {
                        referencedResources.Add(resource);
                    }
                    break;
            }
        }

        return model.Resources
            .OfType<IResourceWithEndpoints>()
            .Where(resource => referencedResources.Contains(resource))
            .Where(resource => resource.HasGraphQLSchema())
            .Select(resource => new GraphQLSourceSchemaResource(
                resource,
                GetSourceSchema(resource)))
            .ToArray();
    }

    public static GraphQLSourceSchemaAnnotation GetSourceSchema(
        IResource resource)
        => resource.Annotations.OfType<GraphQLSourceSchemaAnnotation>().Single();

    public static GraphQLSchemaCompositionAnnotation GetComposition(
        IResource resource)
        => resource.Annotations.OfType<GraphQLSchemaCompositionAnnotation>().Single();

    public static GraphQLProjectSchemaPaths GetProjectSchemaPaths(
        IResource resource,
        GraphQLSourceSchemaAnnotation declaration)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(declaration);

        if (declaration.Location is not SourceSchemaLocationType.ProjectDirectory)
        {
            throw new InvalidOperationException(
                $"GraphQL source resource '{resource.Name}' does not use a project schema file.");
        }

        var projectPath = IOPath.GetFullPath(GetProjectPath(resource));
        var projectDirectory = IOPath.GetDirectoryName(projectPath)!;
        var schemaPath = IOPath.GetFullPath(
            IOPath.Combine(
                projectDirectory,
                declaration.SchemaPath ?? "schema.graphqls"));
        var schemaDirectory = IOPath.GetDirectoryName(schemaPath)!;
        var schemaFileName = IOPath.GetFileNameWithoutExtension(schemaPath);

        return new(
            projectPath,
            schemaPath,
            IOPath.Combine(schemaDirectory, $"{schemaFileName}-settings.json"),
            IOPath.Combine(
                schemaDirectory,
                schemaFileName + "-extensions" + IOPath.GetExtension(schemaPath)));
    }

    [SuppressMessage(
        "Trimming",
        "IL2026:Members attributed with RequiresUnreferencedCodeAttribute require dynamic access otherwise.")]
    public static string GetProjectPath(IResource resource)
    {
        if (resource is not ProjectResource projectResource)
        {
            throw new InvalidOperationException(
                $"GraphQL source resource '{resource.Name}' must be a project resource.");
        }

        return projectResource.GetProjectMetadata().ProjectPath;
    }
}

internal readonly record struct GraphQLSourceSchemaResource(
    IResourceWithEndpoints Resource,
    GraphQLSourceSchemaAnnotation Declaration);

internal readonly record struct GraphQLProjectSchemaPaths(
    string ProjectPath,
    string SchemaPath,
    string SettingsPath,
    string ExtensionsPath);
