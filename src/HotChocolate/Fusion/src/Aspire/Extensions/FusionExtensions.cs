using HotChocolate.Fusion.Aspire;

namespace Aspire.Hosting;

public static class FusionExtensions
{
    public static IResourceBuilder<FusionGatewayResource> AddFusionGateway<TProject>(
        this IDistributedApplicationBuilder builder,
        string name)
        where TProject : IProjectMetadata, new()
        => new FusionGatewayResourceBuilder(builder.AddProject<TProject>(name).WithAnnotation(new FusionGateway()));

    public static IResourceBuilder<FusionGatewayResource> WithSubgraph(
        this IResourceBuilder<FusionGatewayResource> builder,
        IResourceBuilder<ProjectResource> subgraphProject)
        => builder.WithReference(subgraphProject.GetEndpoint("http"));

    public static IResourceBuilder<FusionGatewayResource> WithSubgraph(
        this IResourceBuilder<FusionGatewayResource> builder,
        EndpointReference subgraphEndpoint)
        => builder.WithReference(subgraphEndpoint);

    public static IResourceBuilder<FusionGatewayResource> WithOptions(
        this IResourceBuilder<FusionGatewayResource> builder,
        FusionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        builder.Resource.Annotations.Add(options);
        return builder;
    }
}
