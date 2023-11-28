using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.AspNetCore.TestHost;
using Moq;
using static System.IO.Path;

namespace HotChocolate.OpenApi.Tests;

[Collection("Open api integration tests")]
public class IntegrationTests
{
    [Theory]
    [InlineData("findAnyPets", "query { findPets { name } }")]
    [InlineData("findSinglePet", "query { findPetById(id: 1) { name } }")]
    [InlineData("addPet", """mutation { addPet(input: {name: "Goofy" tag: "Cartoon" }) { name } }""")]
    [InlineData("deletePet", """mutation { deletePet(input: { id: 1}) { success } }""")]
    public async Task QueryPets_Returns_Results(string caseName, string query)
    {
        // Arrange
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var builder = new WebHostBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddControllers();
        });
        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(e => e.MapControllers());
        });
        var openApiServer = new TestServer(builder);

        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() =>
            {
                var client = openApiServer.CreateClient();
                client.BaseAddress = new Uri("http://localhost:5000");
                return client;
            });

        await openApiServer.Host.StartAsync();
        var apiDocument  = await File.ReadAllTextAsync(Combine("__resources__", "PetStore.yaml"));

        var schema = await new ServiceCollection()
            .AddSingleton(httpClientFactoryMock.Object)
            .AddGraphQL()
            .AddOpenApi("PetStore", apiDocument)
            .BuildRequestExecutorAsync();

        // Act
        var result = await schema.ExecuteAsync(QueryRequestBuilder.Create(query));

        // Assert
        Assert.NotNull(result);
        Snapshot.Match(result, postFix: caseName, extension: ".json");
    }


    [Theory]
    [InlineData("me", "query { me { firstName lastName email picture promoCode } }")]
    [InlineData("getProducts", "query { products(longitude: 1, latitude: 1) { productId displayName } }")]
    public async Task QueryUber_Returns_Results(string caseName, string query)
    {
        // Arrange
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var builder = new WebHostBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddControllers();
        });
        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(e => e.MapControllers());
        });
        var openApiServer = new TestServer(builder);

        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => openApiServer.CreateClient());

        await openApiServer.Host.StartAsync();
        var apiDocument  = await File.ReadAllTextAsync(Combine("__resources__", "Uber.json"));

        var schema = await new ServiceCollection()
            .AddSingleton(httpClientFactoryMock.Object)
            .AddGraphQL()
            .AddOpenApi("Uber", apiDocument)
            .BuildRequestExecutorAsync();

        // Act
        var result = await schema.ExecuteAsync(QueryRequestBuilder.Create(query));

        // Assert
        Assert.NotNull(result);
        Snapshot.Match(result, postFix: caseName, extension: ".json");
    }
}
