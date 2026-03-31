using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class CreateMockCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "mock",
                "create",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Create a new mock schema.

            Usage:
              nitro mock create [options]

            Options:
              --api-id <api-id>                   The ID of the API [env: NITRO_API_ID]
              --extension <extension> (REQUIRED)  The path to the graphql file with the schema extension [env: NITRO_SCHEMA_EXTENSION_FILE]
              --schema <schema> (REQUIRED)        The path to the graphql file with the schema [env: NITRO_SCHEMA_FILE]
              --url <url> (REQUIRED)              The URL of the downstream service [env: NITRO_DOWNSTREAM_URL]
              --name <name> (REQUIRED)            The name of the mock schema [env: NITRO_MOCK_SCHEMA_NAME]
              --cloud-url <cloud-url>             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "my-mock")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run
            'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com")
            .ExecuteAsync();

        // assert
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(
            """
            Description:
              Create a new mock schema.

            Usage:
              nitro mock create [options]

            Options:
              --api-id <api-id>                   The ID of the API [env: NITRO_API_ID]
              --extension <extension> (REQUIRED)  The path to the graphql file with the schema extension [env: NITRO_SCHEMA_EXTENSION_FILE]
              --schema <schema> (REQUIRED)        The path to the graphql file with the schema [env: NITRO_SCHEMA_FILE]
              --url <url> (REQUIRED)              The URL of the downstream service [env: NITRO_DOWNSTREAM_URL]
              --name <name> (REQUIRED)            The name of the mock schema [env: NITRO_MOCK_SCHEMA_NAME]
              --cloud-url <cloud-url>             The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                 The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                     The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                      Show help and usage information
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--name' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.CreateMockSchemaAsync(
                "api-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "my-mock",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "my-mock")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Creating mock schema 'my-mock' for API 'api-1'
            └── ✓ Created mock schema 'my-mock'.

            {
              "id": "mock-1",
              "name": "my-mock",
              "url": "https://mock.example.com",
              "downstreamUrl": "https://downstream.example.com/",
              "createdBy": {
                "username": "user1",
                "createdAt": "2025-01-15T10:00:00+00:00"
              },
              "modifiedBy": {
                "username": "user2",
                "modifiedAt": "2025-01-16T10:00:00+00:00"
              }
            }
            """);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task WithOptions_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.CreateMockSchemaAsync(
                "api-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "my-mock",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "my-mock")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "mock-1",
              "name": "my-mock",
              "url": "https://mock.example.com",
              "downstreamUrl": "https://downstream.example.com/",
              "createdBy": {
                "username": "user1",
                "createdAt": "2025-01-15T10:00:00+00:00"
              },
              "modifiedBy": {
                "username": "user2",
                "modifiedAt": "2025-01-16T10:00:00+00:00"
              }
            }
            """);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullMockSchema_ReturnsError_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.CreateMockSchemaAsync(
                "api-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "my-mock",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockPayloadWithNullResult());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "my-mock")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating mock schema 'my-mock' for API 'api-1'
            └── ✕ Failed to create the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the
            expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMockMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        ICreateMockSchema_CreateMockSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.CreateMockSchemaAsync(
                "api-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "my-mock",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "my-mock")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Creating mock schema 'my-mock' for API 'api-1'
            └── ✕ Failed to create the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMockMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        ICreateMockSchema_CreateMockSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.CreateMockSchemaAsync(
                "api-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "my-mock",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "my-mock")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(CreateMockMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        ICreateMockSchema_CreateMockSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.CreateMockSchemaAsync(
                "api-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "my-mock",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMockPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "my-mock")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var result = await RunCreateMockWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"),
            InteractionMode.Interactive);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var result = await RunCreateMockWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"),
            InteractionMode.NonInteractive);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var result = await RunCreateMockWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"),
            InteractionMode.JsonOutput);

        // assert
        result.AssertError(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var result = await RunCreateMockWithException(
            new NitroClientAuthorizationException(),
            InteractionMode.Interactive);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var result = await RunCreateMockWithException(
            new NitroClientAuthorizationException(),
            InteractionMode.NonInteractive);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var result = await RunCreateMockWithException(
            new NitroClientAuthorizationException(),
            InteractionMode.JsonOutput);

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
    }

    private async Task<CommandResult> RunCreateMockWithException(
        Exception ex,
        InteractionMode mode)
    {
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.CreateMockSchemaAsync(
                "api-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "my-mock",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
                "create",
                "--api-id",
                "api-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "my-mock")
            .ExecuteAsync();

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();

        return result;
    }

    public static TheoryData<ICreateMockSchema_CreateMockSchema_Errors, string> CreateMockMutationErrorCases =>
        new()
        {
            {
                new CreateMockSchema_CreateMockSchema_Errors_ApiNotFoundError("ApiNotFoundError", "API not found", "api-1"),
                """
                API not found
                """
            },
            {
                new CreateMockSchema_CreateMockSchema_Errors_MockSchemaNonUniqueNameError("MockSchemaNonUniqueNameError", "Name already in use", "my-mock"),
                """
                Name already in use
                """
            },
            {
                new CreateMockSchema_CreateMockSchema_Errors_UnauthorizedOperation("UnauthorizedOperation", "Not authorized"),
                """
                Not authorized
                """
            },
            {
                new CreateMockSchema_CreateMockSchema_Errors_ValidationError("ValidationError", "Validation failed", []),
                """
                Validation failed
                """
            }
        };

    private static ICreateMockSchema_CreateMockSchema CreateMockSuccessPayload()
    {
        var createdBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_CreatedBy>(MockBehavior.Strict);
        createdBy.SetupGet(x => x.Username).Returns("user1");

        var modifiedBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_ModifiedBy>(MockBehavior.Strict);
        modifiedBy.SetupGet(x => x.Username).Returns("user2");

        var mockSchema = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema>(MockBehavior.Strict);
        mockSchema.SetupGet(x => x.Id).Returns("mock-1");
        mockSchema.SetupGet(x => x.Name).Returns("my-mock");
        mockSchema.SetupGet(x => x.Url).Returns("https://mock.example.com");
        mockSchema.SetupGet(x => x.DownstreamUrl).Returns(new Uri("https://downstream.example.com"));
        mockSchema.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        mockSchema.SetupGet(x => x.CreatedAt).Returns(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero));
        mockSchema.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);
        mockSchema.SetupGet(x => x.ModifiedAt).Returns(new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero));

        var payload = new Mock<ICreateMockSchema_CreateMockSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.MockSchema).Returns(mockSchema.Object);
        payload.SetupGet(x => x.Errors).Returns((IReadOnlyList<ICreateMockSchema_CreateMockSchema_Errors>?)null);

        return payload.Object;
    }

    private static ICreateMockSchema_CreateMockSchema CreateMockPayloadWithNullResult()
    {
        var payload = new Mock<ICreateMockSchema_CreateMockSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.MockSchema).Returns((ICreateMockSchema_CreateMockSchema_MockSchema?)null);
        payload.SetupGet(x => x.Errors).Returns((IReadOnlyList<ICreateMockSchema_CreateMockSchema_Errors>?)null);

        return payload.Object;
    }

    private static ICreateMockSchema_CreateMockSchema CreateMockPayloadWithErrors(
        params ICreateMockSchema_CreateMockSchema_Errors[] errors)
    {
        var payload = new Mock<ICreateMockSchema_CreateMockSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.MockSchema).Returns((ICreateMockSchema_CreateMockSchema_MockSchema?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);

        return payload.Object;
    }
}
