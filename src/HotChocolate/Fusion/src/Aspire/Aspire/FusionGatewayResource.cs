namespace HotChocolate.Fusion.Aspire;

public sealed class FusionGatewayResource(ProjectResource projectResource)
    : Resource(projectResource.Name)
    , IResourceWithEnvironment
    , IResourceWithServiceDiscovery
{
    public ProjectResource ProjectResource { get; } = projectResource;
}