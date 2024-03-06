namespace HotChocolate.Fusion.Analyzers.Tests;

public class Program
{
    public static void Foo(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // resources
        var redis = builder.AddRedisContainer("redis");

        var postgres = builder
            .AddPostgresContainer("postgres")
            .WithPgAdmin()
            .WithAnnotation(
                new ContainerImageAnnotation
                {
                    Image = "ankane/pgvector",
                    Tag = "latest",
                });

        var rabbitMq = builder
            .AddRabbitMQContainer("event-bus")
            .WithEndpoint(containerPort: 58812, name: "management");

        var catalogDb = postgres.AddDatabase("CatalogDB");
        var identityDb = postgres.AddDatabase("IdentityDB");
        var orderingDb = postgres.AddDatabase("OrderingDB");

        // APIs
        var identityApi = builder
            .AddProject<Projects.eShop_Identity_API>("identity-api")
            .WithReference(identityDb)
            .WithLaunchProfile("https");

        var identityHttpsEndpoint = identityApi.GetEndpoint("https");

        var catalogApi = builder
            .AddProject<Projects.eShop_Catalog_API>("catalog-api")
            .WithReference(catalogDb)
            .WithEnvironment("Identity__Url", identityHttpsEndpoint);

        var basketApi = builder
            .AddProject<Projects.eShop_Basket_API>("basket-api")
            .WithReference(redis)
            .WithEnvironment("Identity__Url", identityHttpsEndpoint)
            .WithReference(catalogApi.GetEndpoint("http"));

        var orderingApi = builder
            .AddProject<Projects.eShop_Ordering_API>("ordering-api")
            .WithReference(rabbitMq)
            .WithReference(orderingDb)
            .WithReference(catalogApi.GetEndpoint("http"))
            .WithEnvironment("Identity__Url", identityHttpsEndpoint);
        
        var purchaseApi = builder
            .AddProject<Projects.eShop_Purchase_API>("ordering-api")
            .WithReference(rabbitMq)
            .WithReference(orderingDb)
            .WithReference(catalogApi.GetEndpoint("http"))
            .WithEnvironment("Identity__Url", identityHttpsEndpoint);

        // Fusion
        /*
        builder
            .AddFusionGateway<Projects.eShop_Gateway>("gateway")
            .WithSubgraph(basketApi)
            .WithSubgraph(identityApi.GetEndpoint("http"))
            .WithSubgraph(catalogApi)
            .WithSubgraph(orderingApi)
            .WithSubgraph(purchaseApi)
            .WithEnvironment("Identity__Url", identityHttpsEndpoint);
            */

        builder.Build().Compose().Run();
    }
}

file static class FusionExtensions
{
    public static IResourceBuilder<FusionGatewayResource> AddFusionGateway<TProject>(
        this IDistributedApplicationBuilder builder,
        string name)
        where TProject : IProjectMetadata, new()
        => new FusionGatewayResourceBuilder(builder.AddProject<TProject>(name));

    public static IResourceBuilder<FusionGatewayResource> WithSubgraph(
        this IResourceBuilder<FusionGatewayResource> builder,
        IResourceBuilder<ProjectResource> subgraphProject)
        => builder.WithReference(subgraphProject.GetEndpoint("http"));

    public static IResourceBuilder<FusionGatewayResource> WithSubgraph(
        this IResourceBuilder<FusionGatewayResource> builder,
        EndpointReference subgraphEndpoint)
        => builder.WithReference(subgraphEndpoint);
}

file class FusionGatewayResource(ProjectResource projectResource)
    : Resource(projectResource.Name)
        , IResourceWithEnvironment
        , IResourceWithServiceDiscovery
{
    public ProjectResource ProjectResource { get; } = projectResource;
}

file class FusionGatewayResourceBuilder(
    IResourceBuilder<ProjectResource> projectResourceBuilder)
    : IResourceBuilder<FusionGatewayResource>
{
    public IResourceBuilder<FusionGatewayResource> WithAnnotation<TAnnotation>(TAnnotation annotation)
        where TAnnotation : IResourceAnnotation
    {
        projectResourceBuilder.WithAnnotation(annotation);
        return this;
    }

    public IDistributedApplicationBuilder ApplicationBuilder => projectResourceBuilder.ApplicationBuilder;

    public FusionGatewayResource Resource { get; } = new(projectResourceBuilder.Resource);
}