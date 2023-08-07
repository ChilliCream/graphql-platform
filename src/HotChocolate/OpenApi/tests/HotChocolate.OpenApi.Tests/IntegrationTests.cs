using System.Text.Json;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Xunit;

namespace HotChocolate.OpenApi.Tests;

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

        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => openApiServer.CreateClient());

        await openApiServer.Host.StartAsync();
        await using var stream = File.Open(System.IO.Path.Combine("__resources__", "PetStore.yaml"), FileMode.Open);

        var schema = await new ServiceCollection()
            .AddSingleton(httpClientFactoryMock.Object)
            .AddGraphQL()
            .AddOpenApi(stream, client => client.BaseAddress = new Uri("http://localhost:5000"))
            .UseField(
                next => async context =>
                {
                    await next(context);

                    if (context is { Result: JsonElement element, Selection.IsList: true })
                    {
                        context.Result = element.EnumerateArray();
                    }
                })
            .BuildRequestExecutorAsync();

        // Act
        var result = await schema.ExecuteAsync(QueryRequestBuilder.Create(query));

        // Assert
        Snapshot.Match(result, postFix: caseName, extension: ".json");
    }
}
