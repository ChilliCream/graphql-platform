using Aspire.Hosting.ApplicationModel;
using HotChocolate.Fusion.Aspire;
using HotChocolate.Fusion.Composition;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

public static class FusionExtensions
{
    public static IResourceBuilder<FusionGatewayResource> AddFusionGateway<TProject>(
        this IDistributedApplicationBuilder builder,
        string name)
        where TProject : IProjectMetadata, new()
    {
        var gateway = GatewayInfo.Create<TProject>(name, new FusionCompositionOptions());
        var project = builder.AddProject<TProject>(name).WithAnnotation(gateway);
        return new FusionGatewayResourceBuilder(project);
    }

    public static IResourceBuilder<FusionGatewayResource> WithSubgraph(
        this IResourceBuilder<FusionGatewayResource> builder,
        IResourceBuilder<ProjectResource> subgraphProject)
    {
        var subgraph = new SubgraphInfo(
            subgraphProject.Resource.Name,
            subgraphProject.Resource.GetProjectMetadata().ProjectPath);
        var gateway = builder.GetFusionGatewayMetadata();
        builder.SetFusionGatewayMetadata(gateway with { Subgraphs = gateway.Subgraphs.Add(subgraph) });
        return builder.WithReference(subgraphProject.GetEndpoint("http"));
    }

    public static IResourceBuilder<FusionGatewayResource> WithSubgraph(
        this IResourceBuilder<FusionGatewayResource> builder,
        EndpointReference subgraphEndpoint)
    {
        if (subgraphEndpoint.Resource is ProjectResource subgraphProject)
        {
            var subgraph = new SubgraphInfo(
                subgraphProject.Name,
                subgraphProject.GetProjectMetadata().ProjectPath);
            var gateway = builder.GetFusionGatewayMetadata();
            builder.SetFusionGatewayMetadata(gateway with { Subgraphs = gateway.Subgraphs.Add(subgraph) });
        }

        return builder.WithReference(subgraphEndpoint);
    }

    public static IResourceBuilder<FusionGatewayResource> WithOptions(
        this IResourceBuilder<FusionGatewayResource> builder,
        FusionCompositionOptions compositionOptions)
    {
        var gateway = builder.GetFusionGatewayMetadata();
        builder.SetFusionGatewayMetadata(gateway with { CompositionOptions = compositionOptions });
        return builder;
    }

    private static GatewayInfo GetFusionGatewayMetadata(
        this IResourceBuilder<FusionGatewayResource> fusionResourceBuilder) =>
        fusionResourceBuilder.Resource.GetFusionGatewayMetadata();

    internal static GatewayInfo GetFusionGatewayMetadata(
        this FusionGatewayResource fusionResource) =>
        fusionResource.ProjectResource.Annotations.OfType<GatewayInfo>().First();

    internal static void SetFusionGatewayMetadata(
        this IResourceBuilder<FusionGatewayResource> builder,
        GatewayInfo gateway)
    {
        var resource = builder.Resource.ProjectResource;

        foreach (var annotation in resource.Annotations.OfType<GatewayInfo>().ToArray())
        {
            resource.Annotations.Remove(annotation);
        }

        resource.Annotations.Add(gateway);
    }

    public static DistributedApplication Compose(this DistributedApplication application)
    {
        var options = application.Services.GetRequiredService<DistributedApplicationOptions>();

        if (options.Args is ["compose"])
        {
            var appModel = application.Services.GetRequiredService<DistributedApplicationModel>();
            var gateways = appModel.Resources
                .Where(t => t.Annotations.Any(a => a is GatewayInfo))
                .Select(t => t.Annotations.OfType<GatewayInfo>().First())
                .ToArray();

            if (gateways.Length > 0)
            {
                FusionGatewayConfigurationUtilities.Configure(gateways);
            }

            Environment.Exit(0);
        }

        return application;
    }
}
