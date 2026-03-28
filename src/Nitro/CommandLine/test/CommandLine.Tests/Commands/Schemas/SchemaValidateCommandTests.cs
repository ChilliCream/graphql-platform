using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class SchemaValidateCommandTests
{
    [Fact]
    public async Task Validate_MissingRequiredOptions_ReturnsParseError()
    {
        // arrange
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        var host = CreateHost(client);

        // act
        var exitCode = await host.InvokeAsync("schema", "validate");

        // assert
        Assert.NotEqual(0, exitCode);
        host.StdErr.Trim().MatchInlineSnapshot(
            """
            Option '--stage' is required.
            Option '--api-id' is required.
            Option '--schema-file' is required.
            """);
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Validate_InProgressThenSuccess_ReturnsSuccess()
    {
        // arrange
        var schemaPath = CreateFilePath(".graphql");
        var fileSystem = CreateInputFileSystem((schemaPath, "type Query { hello: String }"));

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaValidationAsync(
                "api-1",
                "prod",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSchemaValidationRequest("request-1"));

        client.Setup(x => x.SubscribeToSchemaValidationAsync("request-1", It.IsAny<CancellationToken>()))
            .Returns(ToValidationUpdates(
                CreateValidationInProgressUpdate(),
                CreateValidationSuccessUpdate()));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "schema",
            "validate",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--schema-file",
            schemaPath);

        // assert
        Assert.Equal(0, exitCode);
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    [Fact]
    public async Task Validate_FailedUpdate_ReturnsError()
    {
        // arrange
        var schemaPath = CreateFilePath(".graphql");
        var fileSystem = CreateInputFileSystem((schemaPath, "type Query { hello: String }"));

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.StartSchemaValidationAsync(
                "api-1",
                "prod",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSchemaValidationRequest("request-2"));

        client.Setup(x => x.SubscribeToSchemaValidationAsync("request-2", It.IsAny<CancellationToken>()))
            .Returns(ToValidationUpdates(
                CreateValidationFailedUpdate("Breaking change", "Syntax error")));

        var host = CreateHost(client, fileSystem);

        // act
        var exitCode = await host.InvokeAsync(
            "schema",
            "validate",
            "--api-id",
            "api-1",
            "--stage",
            "prod",
            "--schema-file",
            schemaPath);

        // assert
        Assert.NotEqual(0, exitCode);
        Assert.Equal(
            $"""
                LOG: Initialized
                LOG: Reading file {schemaPath}
                LOG: Create validation request
                LOG: Validation request created (ID: request-2)
                The schema is invalid:

                Breaking change
                Syntax error
                Validating...

                """.Trim(),
            host.Output.Trim());
        Assert.Empty(host.StdErr);
        client.VerifyAll();
    }

    private static async IAsyncEnumerable<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate>
        ToValidationUpdates(
            params IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate[] updates)
    {
        foreach (var update in updates)
        {
            yield return update;
            await Task.Yield();
        }
    }

    private static IValidateSchemaVersion_ValidateSchema CreateSchemaValidationRequest(string requestId)
    {
        var mock = new Mock<IValidateSchemaVersion_ValidateSchema>();
        mock.SetupGet(x => x.Id).Returns(requestId);
        return mock.Object;
    }

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_ValidationInProgress
        CreateValidationInProgressUpdate()
        => Mock.Of<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_ValidationInProgress>();

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationSuccess
        CreateValidationSuccessUpdate()
        => Mock.Of<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationSuccess>();

    private static IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationFailed
        CreateValidationFailedUpdate(params string[] messages)
    {
        var errors = messages
            .Select(message =>
            {
                var error = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors_UnexpectedProcessingError>();
                error.SetupGet(x => x.Message).Returns(message);
                return (IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_Errors)error.Object;
            })
            .ToArray();

        var update = new Mock<IOnSchemaVersionValidationUpdated_OnSchemaVersionValidationUpdate_SchemaVersionValidationFailed>();
        update.SetupGet(x => x.Errors).Returns(errors);
        return update.Object;
    }

    private static CommandTestHost CreateHost(
        Mock<ISchemasClient> client,
        TestFileSystem? fileSystem = null,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<ISchemasClient>(client.Object)
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
