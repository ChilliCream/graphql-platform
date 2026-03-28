using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class UpdateMockCommandTests
{
    [Fact]
    public async Task Update_MissingId_InNonInteractiveMode_ReturnsError()
    {
        // arrange
        var client = new Mock<IMocksClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync(
            "mock",
            "update",
            "--output",
            "json");

        // assert
        Assert.Equal(1, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            The mock schema ID is required in non-interactive mode.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_WithIdAndOptions_JsonOutput_UsesClient()
    {
        // arrange
        var schemaPath = CreateFilePath(".graphql");
        var extensionPath = CreateFilePath(".graphql");
        var fileSystem = CreateInputFileSystem(
            (schemaPath, "type Query { hello: String }"),
            (extensionPath, "extend type Query { world: String }"));
        var createdAt = new DateTimeOffset(2026, 03, 27, 0, 0, 0, TimeSpan.Zero);

        var client = new Mock<IMocksClient>(MockBehavior.Strict);
        client.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                It.IsAny<Stream>(),
                "https://downstream.local/v2",
                It.IsAny<Stream>(),
                "mock-schema-updated",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockResult(
                "mock-1",
                "mock-schema-updated",
                "https://mock.local/graphql",
                "https://downstream.local/v2",
                "alice",
                createdAt,
                "bob",
                createdAt.AddHours(2)));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "mock",
            "update",
            "mock-1",
            "--schema",
            schemaPath,
            "--extension",
            extensionPath,
            "--url",
            "https://downstream.local/v2",
            "--name",
            "mock-schema-updated",
            "--output",
            "json");

        // assert
        Assert.Equal(0, exitCode);
                host.Output.Trim().MatchInlineSnapshot(
                        """
                        {
                            "id": "mock-1",
                            "name": "mock-schema-updated",
                            "url": "https://mock.local/graphql",
                            "downstreamUrl": "https://downstream.local/v2",
                            "createdBy": {
                                "username": "alice",
                                "createdAt": "2026-03-27T00:00:00+00:00"
                            },
                            "modifiedBy": {
                                "username": "bob",
                                "modifiedAt": "2026-03-27T02:00:00+00:00"
                            }
                        }
                        """);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static CommandBuilder CreateHost(
        Mock<IMocksClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
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

    private static IUpdateMockSchema_UpdateMockSchema CreateMockResult(
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

        var mockSchema = new Mock<IUpdateMockSchema_UpdateMockSchema_MockSchema_MockSchema>();
        mockSchema.SetupGet(x => x.Id).Returns(id);
        mockSchema.SetupGet(x => x.Name).Returns(name);
        mockSchema.SetupGet(x => x.Url).Returns(url);
        mockSchema.SetupGet(x => x.DownstreamUrl).Returns(new Uri(downstreamUrl));
        mockSchema.SetupGet(x => x.CreatedAt).Returns(createdAt);
        mockSchema.SetupGet(x => x.ModifiedAt).Returns(modifiedAt);
        mockSchema.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        mockSchema.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);

        var payload = new Mock<IUpdateMockSchema_UpdateMockSchema>();
        payload.SetupGet(x => x.MockSchema).Returns(mockSchema.Object);
        payload.SetupGet(x => x.Errors).Returns([]);

        return payload.Object;
    }
}
