namespace HotChocolate.Fusion.Aspire;

public sealed class FusionGatewayResourceBuilder(
    IResourceBuilder<ProjectResource> projectResourceBuilder)
    : IResourceBuilder<FusionGatewayResource>
{
    public IResourceBuilder<FusionGatewayResource> WithAnnotation<TAnnotation>(
        TAnnotation annotation,
        ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append) 
        where TAnnotation : IResourceAnnotation
    {
        projectResourceBuilder.WithAnnotation(annotation, behavior);
        return this;
    }

    public IDistributedApplicationBuilder ApplicationBuilder => projectResourceBuilder.ApplicationBuilder;

    public FusionGatewayResource Resource { get; } = new(projectResourceBuilder.Resource);
}