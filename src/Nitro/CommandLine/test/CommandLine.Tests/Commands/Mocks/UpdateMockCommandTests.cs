using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.Client.Mocks;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Services;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Mocks;

public sealed class UpdateMockCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "mock",
                "update",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Update a mock schema with a new schema and extension file.

            Usage:
              nitro mock update [<id>] [options]

            Arguments:
              <id>  The resource ID

            Options:
              --extension <extension>  The path to the graphql file with the schema extension [env: NITRO_SCHEMA_EXTENSION_FILE]
              --schema <schema>        The path to the graphql file with the schema [env: NITRO_SCHEMA_FILE]
              --url <url>              The URL of the downstream service [env: NITRO_DOWNSTREAM_URL]
              --name <name>            The name of the mock schema [env: NITRO_MOCK_SCHEMA_NAME]
              --cloud-url <cloud-url>  The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>      The API key used for authentication [env: NITRO_API_KEY]
              --output <json>          The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help           Show help and usage information

            Example:
              nitro mock update "<mock-schema-id>" \
                --extension "./extension.graphql" \
                --schema "./schema.graphqls"
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
                "update",
                "mock-1")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingId_ReturnsError(InteractionMode mode)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "mock",
                "update")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The 'id' option is required in non-interactive mode.
            """);

        apisClient.VerifyAll();
        mocksClient.VerifyAll();
    }

    [Fact]
    public async Task WithId_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "updated-mock",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "update",
                "mock-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "updated-mock")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Updating mock schema 'mock-1'
            └── ✓ Updated mock schema 'mock-1'.

            {
              "id": "mock-1",
              "name": "updated-mock",
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
    public async Task WithId_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        var extensionStream = new MemoryStream();
        var schemaStream = new MemoryStream();
        fileSystem.Setup(x => x.OpenReadStream("ext.graphql")).Returns(extensionStream);
        fileSystem.Setup(x => x.OpenReadStream("schema.graphql")).Returns(schemaStream);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                schemaStream,
                "https://downstream.example.com",
                extensionStream,
                "updated-mock",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mock",
                "update",
                "mock-1",
                "--extension",
                "ext.graphql",
                "--schema",
                "schema.graphql",
                "--url",
                "https://downstream.example.com",
                "--name",
                "updated-mock")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {
              "id": "mock-1",
              "name": "updated-mock",
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
    public async Task WithIdOnly_NoOptionalFiles_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "update",
                "mock-1")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Updating mock schema 'mock-1'
            └── ✓ Updated mock schema 'mock-1'.

            {
              "id": "mock-1",
              "name": "updated-mock",
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

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockPayloadWithNullResult());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "update",
                "mock-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating mock schema 'mock-1'
            └── ✕ Failed to update the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UpdateMockMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IUpdateMockSchema_UpdateMockSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "update",
                "mock-1")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating mock schema 'mock-1'
            └── ✕ Failed to update the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UpdateMockMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_Interactive(
        IUpdateMockSchema_UpdateMockSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddSessionWithWorkspace()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "mock",
                "update",
                "mock-1")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UpdateMockMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_JsonOutput(
        IUpdateMockSchema_UpdateMockSchema_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                null,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "mock",
                "update",
                "mock-1")
            .ExecuteAsync();

        // assert
        result.AssertError(expectedStdErr);

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var result = await RunUpdateMockWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"),
            mode);

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
        var result = await RunUpdateMockWithException(
            new NitroClientGraphQLException("Some message.", "SOME_CODE"),
            InteractionMode.NonInteractive);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating mock schema 'mock-1'
            └── ✕ Failed to update the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var result = await RunUpdateMockWithException(
            new NitroClientAuthorizationException(),
            mode);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var result = await RunUpdateMockWithException(
            new NitroClientAuthorizationException(),
            InteractionMode.NonInteractive);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Updating mock schema 'mock-1'
            └── ✕ Failed to update the mock schema.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    private async Task<CommandResult> RunUpdateMockWithException(
        Exception ex,
        InteractionMode mode)
    {
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                null,
                null,
                null,
                null,
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
                "update",
                "mock-1")
            .ExecuteAsync();

        mocksClient.VerifyAll();
        fileSystem.VerifyAll();

        return result;
    }

    [Fact]
    public async Task Update_Should_UpdateOnlyName_When_OnlyNameProvided()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                null,
                null,
                null,
                "new-name",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "update",
                "mock-1",
                "--name",
                "new-name")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Updating mock schema 'mock-1'
            └── ✓ Updated mock schema 'mock-1'.

            {
              "id": "mock-1",
              "name": "updated-mock",
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
    public async Task Update_Should_UpdateOnlyUrl_When_OnlyUrlProvided()
    {
        // arrange
        var apisClient = new Mock<IApisClient>(MockBehavior.Strict);
        var mocksClient = new Mock<IMocksClient>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);

        mocksClient.Setup(x => x.UpdateMockSchemaAsync(
                "mock-1",
                null,
                "https://new-downstream.example.com",
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUpdateMockSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(apisClient.Object)
            .AddService(mocksClient.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "mock",
                "update",
                "mock-1",
                "--url",
                "https://new-downstream.example.com")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Updating mock schema 'mock-1'
            └── ✓ Updated mock schema 'mock-1'.

            {
              "id": "mock-1",
              "name": "updated-mock",
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

    public static TheoryData<IUpdateMockSchema_UpdateMockSchema_Errors, string> UpdateMockMutationErrorCases =>
        new()
        {
            {
                new UpdateMockSchema_UpdateMockSchema_Errors_MockSchemaNotFoundError("MockSchemaNotFoundError", "Mock schema not found"),
                """
                Mock schema not found
                """
            },
            {
                new UpdateMockSchema_UpdateMockSchema_Errors_MockSchemaNonUniqueNameError("MockSchemaNonUniqueNameError", "Name already in use", "my-mock"),
                """
                Name already in use
                """
            },
            {
                new UpdateMockSchema_UpdateMockSchema_Errors_UnauthorizedOperation("UnauthorizedOperation", "Not authorized"),
                """
                Not authorized
                """
            },
            {
                new UpdateMockSchema_UpdateMockSchema_Errors_ValidationError("ValidationError", "Validation failed", []),
                """
                Validation failed
                """
            }
        };

    private static IUpdateMockSchema_UpdateMockSchema CreateUpdateMockSuccessPayload()
    {
        var createdBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_CreatedBy>(MockBehavior.Strict);
        createdBy.SetupGet(x => x.Username).Returns("user1");

        var modifiedBy = new Mock<ICreateMockSchema_CreateMockSchema_MockSchema_ModifiedBy>(MockBehavior.Strict);
        modifiedBy.SetupGet(x => x.Username).Returns("user2");

        var mockSchema = new Mock<IUpdateMockSchema_UpdateMockSchema_MockSchema>(MockBehavior.Strict);
        mockSchema.SetupGet(x => x.Id).Returns("mock-1");
        mockSchema.SetupGet(x => x.Name).Returns("updated-mock");
        mockSchema.SetupGet(x => x.Url).Returns("https://mock.example.com");
        mockSchema.SetupGet(x => x.DownstreamUrl).Returns(new Uri("https://downstream.example.com"));
        mockSchema.SetupGet(x => x.CreatedBy).Returns(createdBy.Object);
        mockSchema.SetupGet(x => x.CreatedAt).Returns(new DateTimeOffset(2025, 1, 15, 10, 0, 0, TimeSpan.Zero));
        mockSchema.SetupGet(x => x.ModifiedBy).Returns(modifiedBy.Object);
        mockSchema.SetupGet(x => x.ModifiedAt).Returns(new DateTimeOffset(2025, 1, 16, 10, 0, 0, TimeSpan.Zero));

        var payload = new Mock<IUpdateMockSchema_UpdateMockSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.MockSchema).Returns(mockSchema.Object);
        payload.SetupGet(x => x.Errors).Returns((IReadOnlyList<IUpdateMockSchema_UpdateMockSchema_Errors>?)null);

        return payload.Object;
    }

    private static IUpdateMockSchema_UpdateMockSchema CreateUpdateMockPayloadWithNullResult()
    {
        var payload = new Mock<IUpdateMockSchema_UpdateMockSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.MockSchema).Returns((IUpdateMockSchema_UpdateMockSchema_MockSchema?)null);
        payload.SetupGet(x => x.Errors).Returns((IReadOnlyList<IUpdateMockSchema_UpdateMockSchema_Errors>?)null);

        return payload.Object;
    }

    private static IUpdateMockSchema_UpdateMockSchema CreateUpdateMockPayloadWithErrors(
        params IUpdateMockSchema_UpdateMockSchema_Errors[] errors)
    {
        var payload = new Mock<IUpdateMockSchema_UpdateMockSchema>(MockBehavior.Strict);
        payload.SetupGet(x => x.MockSchema).Returns((IUpdateMockSchema_UpdateMockSchema_MockSchema?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);

        return payload.Object;
    }
}
