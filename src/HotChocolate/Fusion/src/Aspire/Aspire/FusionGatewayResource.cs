using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace HotChocolate.Fusion.Aspire;

public sealed class FusionGatewayResource(ProjectResource projectResource)
    : Resource(projectResource.Name)
    , IResourceWithEnvironment
    , IResourceWithServiceDiscovery
{
    public ProjectResource ProjectResource { get; } = projectResource;

    public EndpointReference GetEndpoint(string endpointName)
        => ((IResourceWithEndpoints)ProjectResource).GetEndpoint(endpointName);
}
