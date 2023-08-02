using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using Microsoft.AspNetCore.TestHost;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.OpenApi.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task QueryPets_Returns_Results()
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
            .BuildRequestExecutorAsync();

        // Act
        var result = await schema.ExecuteAsync(QueryRequestBuilder.Create("query { findpets { name } }"));

        // Assert
        result.MatchSnapshot();
    }
}
