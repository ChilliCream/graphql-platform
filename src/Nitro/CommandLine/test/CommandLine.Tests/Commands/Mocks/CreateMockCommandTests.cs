using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class CreateMockCommandTests
{
    [Fact]
    public async Task Create_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IMocksClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("mock", "create");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--extension' is required.
            Option '--schema' is required.
            Option '--url' is required.
            Option '--name' is required.


            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_WithOptions_JsonOutput_UsesClient()
    {
        // arrange
        var schemaPath = CreateFilePath(".graphql");
        var extensionPath = CreateFilePath(".graphql");
        var fileSystem = CreateInputFileSystem(
            (schemaPath, "type Query { hello: String }"),
            (extensionPath, "extend type Query { world: String }"));
        var createdAt = new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero);

        var client = new Mock<IMocksClient>(MockBehavior.Strict);
        client.Setup(x => x.CreateMockSchemaAsync(
                "api-1",
                It.IsAny<Stream>(),
                "https://downstream.local/graphql",
                It.IsAny<Stream>(),
                "mock-schema",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockResult(
                "mock-1",
                "mock-schema",
                "https://mock.local/graphql",
                "https://downstream.local/graphql",
                "alice",
                createdAt,
                "bob",
                createdAt.AddHours(1)));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "mock",
            "create",
            "--api-id",
            "api-1",
            "--api-key",
            "test-api-key",
            "--schema",
            schemaPath,
            "--extension",
            extensionPath,
            "--url",
            "https://downstream.local/graphql",
            "--name",
            "mock-schema",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
                host.Output.Trim().MatchInlineSnapshot(
                        """
                        {
                            "id": "mock-1",
                            "name": "mock-schema",
                            "url": "https://mock.local/graphql",
                            "downstreamUrl": "https://downstream.local/graphql",
                            "createdBy": {
                                "username": "alice",
                                "createdAt": "2026-03-27T00:00:00+00:00"
                            },
                            "modifiedBy": {
                                "username": "bob",
                                "modifiedAt": "2026-03-27T01:00:00+00:00"
                            }
                        }
                        """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandTestHost CreateHost(
        Mock<IMocksClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        if (fileSystem is not null)
        {
            host.AddService<IFileSystem>(fileSystem);
        }

        return host;
    }

    private static string CreateFilePath(string extension)
        => Path.Combine("/tmp", Path.GetRandomFileName() + extension);

    private static TestFileSystem CreateInputFileSystem(params (string path, string content)[] seededFiles)
        => new(seededFiles.Select(x => new KeyValuePair<string, string>(x.path, x.content)).ToArray());

    private static ICreateMockSchema_CreateMockSchema CreateMockResult(
        string id,
        string name,
        string url,
        string downstreamUrl,
        string createdByUsername,
        DateTimeOffset createdAt,
        string modifiedByUsername,
        DateTimeOffset modifiedAt)
    {
        var createdBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_CreatedBy_UserInfo>();
        createdBy.SetupGet(x => x.Username).Returns(createdByUsername);

        var modifiedBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_ModifiedBy_UserInfo>();
        modifiedBy.SetupGet(x => x.Username).Returns(modifiedByUsername);

        var mockSchema = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_MockSchema>();
        mockSchema.SetupGet(x => x.Id).Returns(id);
        mockSchema.SetupGet(x => x.Name).Returns(name);
        mockSchema.SetupGet(x => x.Url).Returns(url);
        mockSchema.SetupGet(x => x.DownstreamUrl).Returns(new Uri(downstreamUrl));
        mockSchema.SetupGet(x => x.CreatedAt).Returns(createdAt);
        mockSchema.SetupGet(x => x.ModifiedAt).Returns(modifiedAt);
        mockSchema.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        mockSchema.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);

        var payload = new Mock<ICreateMockSchema_CreateMockSchema>();
        payload.SetupGet(x => x.MockSchema).Returns(mockSchema.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
