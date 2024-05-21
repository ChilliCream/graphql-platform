using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.OpenApi.Extensions;
using Microsoft.AspNetCore.TestHost;
using Moq;

namespace HotChocolate.OpenApi.Tests;

public sealed class IntegrationTests
{
    [Theory]
    [MemberData(nameof(OperationsWithoutMutationConventions))]
    public async Task ExecuteQuery_PetStoreExpandedWithoutMutationConventions_ReturnsExpectedResult(
        string caseName,
        string query)
    {
        // Arrange
        var openApiServer = CreateOpenApiServer();
        var httpClientFactoryMock = CreateHttpClientFactoryMock(openApiServer);

        await openApiServer.Host.StartAsync();
        var openApiDocument = FileResource.Open("petstore-expanded.yaml");

        var schema = await new ServiceCollection()
            .AddSingleton(httpClientFactoryMock.Object)
            .AddGraphQL()
            .AddOpenApi("PetStoreExpanded", openApiDocument, enableMutationConventions: false)
            .BuildRequestExecutorAsync();

        // Act
        var result = await schema.ExecuteAsync(query);

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
        var apiDocument  = FileResource.Open("Uber.json");

        var schema = await new ServiceCollection()
            .AddSingleton(httpClientFactoryMock.Object)
            .AddGraphQL()
            .AddOpenApi("Uber", apiDocument)
            .BuildRequestExecutorAsync();

        // Act
        var result = await schema.ExecuteAsync(query);

        // Assert
        Assert.NotNull(result);
        Snapshot.Match(result, postFix: caseName, extension: ".json");
    }

    [Theory]
    [MemberData(nameof(OperationsWithMutationConventions))]
    public async Task ExecuteQuery_PetStoreExpandedWithMutationConventions_ReturnsExpectedResult(
        string caseName,
        string query)
    {
        // Arrange
        var openApiServer = CreateOpenApiServer();
        var httpClientFactoryMock = CreateHttpClientFactoryMock(openApiServer);

        await openApiServer.Host.StartAsync();
        var openApiDocument = FileResource.Open("petstore-expanded.yaml");

        var schema = await new ServiceCollection()
            .AddSingleton(httpClientFactoryMock.Object)
            .AddGraphQL()
            .AddOpenApi("PetStoreExpanded", openApiDocument, enableMutationConventions: true)
            .BuildRequestExecutorAsync();

        // Act
        var result = await schema.ExecuteAsync(query);

        // Assert
        Assert.NotNull(result);
        Snapshot.Match(result, postFix: caseName, extension: ".json");
    }

    [Theory]
    [MemberData(nameof(OperationsWithLinks))]
    public async Task ExecuteQuery_SyntheticWithLinks_ReturnsExpectedResult(
        string caseName,
        string query)
    {
        // Arrange
        var openApiServer = CreateOpenApiServer();
        var httpClientFactoryMock = CreateHttpClientFactoryMock(openApiServer);

        await openApiServer.Host.StartAsync();
        var openApiDocument = FileResource.Open("synthetic-with-links.yaml");

        var schema = await new ServiceCollection()
            .AddSingleton(httpClientFactoryMock.Object)
            .AddGraphQL()
            .AddOpenApi("SyntheticWithLinks", openApiDocument, enableMutationConventions: true)
            .BuildRequestExecutorAsync();

        // Act
        var result = await schema.ExecuteAsync(query);

        // Assert
        httpClientFactoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Exactly(2));
        Assert.NotNull(result);
        Snapshot.Match(result, postFix: caseName, extension: ".json");
    }

    private static TestServer CreateOpenApiServer()
    {
        var builder = new WebHostBuilder();

        builder
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddControllers();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(e => e.MapControllers());
            });

        return new TestServer(builder);
    }

    private static Mock<IHttpClientFactory> CreateHttpClientFactoryMock(TestServer openApiServer)
    {
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();

        httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() =>
            {
                var client = openApiServer.CreateClient();
                client.BaseAddress = new Uri("http://localhost:5000");

                return client;
            });

        return httpClientFactoryMock;
    }

    private static TheoryData<string, string> OperationsWithoutMutationConventions()
    {
        return new TheoryData<string, string>
        {
            {
                "addPet_Success",
                """
                mutation {
                    addPet(newPet: { name: "Goofy", tag: "Cartoon" }) {
                        ... on Pet { id, name, tag }
                    }
                }
                """
            },
            {
                "addPet_Failure",
                """
                mutation {
                    addPet(newPet: { name: "Goofy", tag: "" }) {
                        ... on Error { code, message }
                    }
                }
                """
            },
            {
                "findPetById_Success",
                """
                query {
                    findPetById(id: 1) {
                        ... on Pet { id, name, tag }
                    }
                }
                """
            },
            {
                "findPetById_Failure",
                """
                query {
                    findPetById(id: 100) {
                        ... on Error { code, message }
                    }
                }
                """
            },
            {
                "findPets_Success",
                """
                query {
                    findPets(tags: ["cat", "dog"], limit: 2) {
                        ... on PetListWrapper { value { id, name, tag } }
                    }
                }
                """
            },
            {
                "findPets_Failure",
                """
                query {
                    findPets(tags: ["cat", "dog"], limit: 20) {
                        ... on Error { code, message }
                    }
                }
                """
            },
            {
                "deletePet_Success",
                """
                mutation {
                    deletePet(id: 3) {
                        ... on JsonWrapper { value }
                    }
                }
                """
            },
            {
                "deletePet_Failure",
                """
                mutation {
                    deletePet(id: 100) {
                        ... on Error { code, message }
                    }
                }
                """
            },
        };
    }

    private static TheoryData<string, string> OperationsWithMutationConventions()
    {
        return new TheoryData<string, string>
        {
            {
                "addPet_Success",
                """
                mutation {
                    addPet(input: { name: "Goofy", tag: "Cartoon" }) {
                        pet { id, name, tag }
                    }
                }
                """
            },
            {
                "addPet_Failure",
                """
                mutation {
                    addPet(input: { name: "Goofy", tag: "" }) {
                        errors {
                            ... on Error { code, message }
                        }
                    }
                }
                """
            },
            {
                "findPetById_Success",
                """
                query {
                    findPetById(id: 1) {
                        ... on Pet { id, name, tag }
                    }
                }
                """
            },
            {
                "findPetById_Failure",
                """
                query {
                    findPetById(id: 100) {
                        ... on Error { code, message }
                    }
                }
                """
            },
            {
                "findPets_Success",
                """
                query {
                    findPets(tags: ["cat", "dog"], limit: 2) {
                        ... on PetListWrapper { value { id, name, tag } }
                    }
                }
                """
            },
            {
                "findPets_Failure",
                """
                query {
                    findPets(tags: ["cat", "dog"], limit: 20) {
                        ... on Error { code, message }
                    }
                }
                """
            },
            {
                "deletePet_Success",
                """
                mutation {
                    deletePet(id: 3) {
                        json
                    }
                }
                """
            },
            {
                "deletePet_Failure",
                """
                mutation {
                    deletePet(id: 100) {
                        errors {
                            ... on Error { code, message }
                        }
                    }
                }
                """
            },
        };
    }

    private static TheoryData<string, string> OperationsWithLinks()
    {
        return new TheoryData<string, string>
        {
            {
                "getArticles",
                """
                query {
                    a: getArticles {
                        id
                        title
                        author { id, username } # Link
                    }

                    b: getArticles {
                        id
                        title
                        author { id, username } # Link
                    }
                }
                """
            },
            {
                "getArticleById",
                """
                query {
                    a: getArticleById(id: 1) {
                        id
                        title
                        author { id, username } # Link
                    }

                    b: getArticleById(id: 1) {
                        id
                        title
                        author { id, username } # Link
                    }
                }
                """
            },
        };
    }
}
