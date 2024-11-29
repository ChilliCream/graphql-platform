using HotChocolate.Fusion.Aspire;

namespace HotChocolate.Fusion.Analyzers.Tests;

public static class Program
{
    public static void Foo(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        // resources
        var redis = builder
            .AddRedis("redis");

        var postgres = builder
            .AddPostgres("postgres")
            .WithPgAdmin()
            .WithAnnotation(
                new ContainerImageAnnotation
                {
                    Image = "ankane/pgvector",
                    Tag = "latest",
                });

        var rabbitMq = builder
            .AddRabbitMQ("event-bus")
            .WithEndpoint(port: 58812, name: "management");

        var catalogDb = postgres.AddDatabase("CatalogDB");
        var identityDb = postgres.AddDatabase("IdentityDB");
        var orderingDb = postgres.AddDatabase("OrderingDB");

        // APIs
        var identityApi = builder
            .AddProject<Projects.eShop_Identity_API>("identity-api", launchProfileName: "https")
            .WithReference(identityDb);

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
        var gateway = builder
            .AddFusionGateway<Projects.eShop_Gateway>("gateway")
            .WithOptions(new FusionCompositionOptions { EnableGlobalObjectIdentification = true })
            .WithSubgraph(basketApi)
            .WithSubgraph(identityApi)
            .WithSubgraph(catalogApi)
            .WithSubgraph(orderingApi)
            .WithSubgraph(purchaseApi);

        builder.Build().Compose().Run();
    }
}
