using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Clients;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Clients;

public sealed class ValidateClientCommandTests
{
    [Fact]
    public async Task Validate_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("client", "validate");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--stage' is required.
            Option '--client-id' is required.
            Option '--operations-file' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Validate_InProgressThenSuccess_ReturnsSuccess()
    {
        // arrange
        var operationsPath = CreateFilePath(".json");
        var fileSystem = CreateInputFileSystem((operationsPath, "{\"doc-1\":\"query A { field }\"}"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientValidationAsync(
                "client-1",
                "prod",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientValidationRequest("request-1"));

        client.Setup(x => x.SubscribeToClientValidationAsync("request-1", It.IsAny<CancellationToken>()))
            .Returns(ToValidationUpdates(
                CreateValidationInProgressUpdate(),
                CreateValidationSuccessUpdate()));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "validate",
            "--client-id",
            "client-1",
            "--stage",
            "prod",
            "--operations-file",
            operationsPath);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Validate_FailedUpdate_ReturnsError()
    {
        // arrange
        var operationsPath = CreateFilePath(".json");
        var fileSystem = CreateInputFileSystem((operationsPath, "{\"doc-1\":\"query A { field }\"}"));

        var client = new Mock<IClientsClient>(MockBehavior.Strict);
        client.Setup(x => x.StartClientValidationAsync(
                "client-1",
                "prod",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateClientValidationRequest("request-2"));

        client.Setup(x => x.SubscribeToClientValidationAsync("request-2", It.IsAny<CancellationToken>()))
            .Returns(ToValidationUpdates(
                CreateValidationFailedUpdate("Persisted query conflict", "Unknown field in operation")));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "client",
            "validate",
            "--client-id",
            "client-1",
            "--stage",
            "prod",
            "--operations-file",
            operationsPath);

        // assert
        Assert.NotEqual(0, exitCode);
        Assert.Equal(
            $"""
                LOG: Initialized
                LOG: Reading file {operationsPath}
                LOG: Create validation request
                LOG: Validation request created (ID: request-2)
                The client is invalid:

                Persisted query conflict
                Unknown field in operation
                Validating...

                """.Trim(),
            host.Output.Trim());
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static async IAsyncEnumerable<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate>
        ToValidationUpdates(
            params IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate[] updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private static IValidateClientVersion_ValidateClient CreateClientValidationRequest(string requestId)
    {
        var mock = new Mock<IValidateClientVersion_ValidateClient>();
        mock.SetupGet(x => x.Id).Returns(requestId);
        return mock.Object;
    }

    private static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ValidationInProgress
        CreateValidationInProgressUpdate()
        => Mock.Of<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ValidationInProgress>();

    private static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ClientVersionValidationSuccess
        CreateValidationSuccessUpdate()
        => Mock.Of<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ClientVersionValidationSuccess>();

    private static IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ClientVersionValidationFailed
        CreateValidationFailedUpdate(params string[] messages)
    {
        var errors = messages
            .Select(message =>
            {
                var error = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_UnexpectedProcessingError>();
                error.SetupGet(x => x.Message).Returns(message);
                return (IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors)error.Object;
            })
            .ToArray();

        var update = new Mock<IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_ClientVersionValidationFailed>();
        update.SetupGet(x => x.Errors).Returns(errors);
        return update.Object;
    }

    private static CommandBuilder CreateHost(
        Mock<IClientsClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandBuilder()
            .AddService<IClientsClient>(client.Object)
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
}
