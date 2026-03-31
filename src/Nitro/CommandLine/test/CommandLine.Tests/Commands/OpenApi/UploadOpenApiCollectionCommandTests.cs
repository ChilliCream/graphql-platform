using System.Net;
using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.OpenApi;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.OpenApi;

public sealed class UploadOpenApiCollectionCommandTests(NitroCommandFixture fixture) : IClassFixture<NitroCommandFixture>
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder(fixture)
            .AddArguments(
                "openapi",
                "upload",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Upload a new OpenAPI collection version.

            Usage:
              nitro openapi upload [options]

            Options:
              --tag <tag> (REQUIRED)                                      The tag of the schema version to deploy [env: NITRO_TAG]
              --openapi-collection-id <openapi-collection-id> (REQUIRED)  The ID of the OpenAPI collection [env: NITRO_OPENAPI_COLLECTION_ID]
              -p, --pattern <pattern> (REQUIRED)                          One or more glob patterns for selecting OpenAPI document files
              --cloud-url <cloud-url>                                     The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                         The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                             The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                              Show help and usage information
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
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
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
                "openapi",
                "upload")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--tag' is required.
            Option '--openapi-collection-id' is required.
            Option '--pattern' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task NoFilesFound_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        var fileSystem = CreateEmptyFileSystem();

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not find any OpenAPI documents with the provided pattern.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new OpenAPI collection version 'v1' for collection 'oa-1'
            └── ✕ Failed to upload a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientGraphQLException("Some message.", "SOME_CODE"));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned an unexpected GraphQL error: Some message. (SOME_CODE)
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new OpenAPI collection version 'v1' for collection 'oa-1'
            └── ✕ Failed to upload a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsAuthorizationException_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientAuthorizationException());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key
            has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsRequestEntityTooLarge_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientHttpRequestException(HttpStatusCode.RequestEntityTooLarge));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new OpenAPI collection version 'v1' for collection 'oa-1'
            └── ✕ Failed to upload a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned a 413 (Request Entity Too Large) HTTP status code. If you
            are running a self-hosted instance, check your ingress controller body-size
            limits, reverse proxy settings, or load balancer request size limits.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ClientThrowsRequestEntityTooLarge_ReturnsError(InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(new NitroClientHttpRequestException(HttpStatusCode.RequestEntityTooLarge));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The server returned a 413 (Request Entity Too Large) HTTP status code. If you
            are running a self-hosted instance, check your ingress controller body-size
            limits, reverse proxy settings, or load balancer request size limits.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCases))]
    public async Task MutationReturnsTypedError_ReturnsError_NonInteractive(
        IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors mutationError,
        string expectedStdErr)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new OpenAPI collection version 'v1' for collection 'oa-1'
            └── ✕ Failed to upload a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(UploadMutationErrorCasesWithModes))]
    public async Task MutationReturnsTypedError_ReturnsError(
        IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors mutationError,
        string expectedStdErr,
        InteractionMode mode)
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithErrors(mutationError));

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdErr.MatchInlineSnapshot(expectedStdErr);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task MutationReturnsNullVersion_ReturnsError_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadPayloadWithNullVersion());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Uploading new OpenAPI collection version 'v1' for collection 'oa-1'
            └── ✕ Failed to upload a new OpenAPI collection version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not upload OpenAPI collection version.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_ReturnsSuccess_NonInteractive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            Uploading new OpenAPI collection version 'v1' for collection 'oa-1'
            └── ✓ Uploaded new OpenAPI collection version 'v1'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_ReturnsSuccess_Interactive()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.AssertSuccess();

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_ReturnsSuccess_JsonOutput()
    {
        // arrange
        var (client, fileSystem) = CreateUploadSetup(CreateUploadSuccessPayload());

        // act
        var result = await new CommandBuilder(fixture)
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "openapi",
                "upload",
                "--tag",
                "v1",
                "--openapi-collection-id",
                "oa-1",
                "--pattern",
                "*.openapi.json")
            .ExecuteAsync();

        // assert
        result.AssertSuccess(
            """
            {}
            """);

        client.VerifyAll();
    }

    private static readonly byte[] _validOpenApiGraphql =
        """query GetUsers @http(method: GET, route: "/users") { users { id } }"""u8.ToArray();

    private static Mock<IFileSystem> CreateOpenApiFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GlobMatch(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns(["api.openapi.json"]);
        fileSystem.Setup(x => x.ReadAllBytesAsync("api.openapi.json", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_validOpenApiGraphql);
        return fileSystem;
    }

    private static Mock<IFileSystem> CreateEmptyFileSystem()
    {
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.GlobMatch(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns([]);
        return fileSystem;
    }

    private static (Mock<IOpenApiClient> Client, Mock<IFileSystem> FileSystem) CreateUploadSetup(
        IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection payload)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadOpenApiCollectionVersionAsync(
                "oa-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(payload);

        var fileSystem = CreateOpenApiFileSystem();

        return (client, fileSystem);
    }

    private static (Mock<IOpenApiClient> Client, Mock<IFileSystem> FileSystem) CreateUploadSetup(
        Exception ex)
    {
        var client = new Mock<IOpenApiClient>(MockBehavior.Strict);
        client.Setup(x => x.UploadOpenApiCollectionVersionAsync(
                "oa-1",
                "v1",
                It.IsAny<Stream>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);

        var fileSystem = CreateOpenApiFileSystem();

        return (client, fileSystem);
    }

    private static IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection CreateUploadSuccessPayload()
    {
        var version = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_OpenApiCollectionVersion>(MockBehavior.Strict);
        version.SetupGet(x => x.Id).Returns("oav-1");

        var payload = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.OpenApiCollectionVersion).Returns(version.Object);

        return payload.Object;
    }

    private static IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection CreateUploadPayloadWithNullVersion()
    {
        var payload = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors>?)null);
        payload.SetupGet(x => x.OpenApiCollectionVersion)
            .Returns((IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_OpenApiCollectionVersion?)null);

        return payload.Object;
    }

    private static IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection CreateUploadPayloadWithErrors(
        params IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors[] errors)
    {
        var payload = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection>(MockBehavior.Strict);
        payload.SetupGet(x => x.Errors).Returns(errors);
        payload.SetupGet(x => x.OpenApiCollectionVersion)
            .Returns((IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_OpenApiCollectionVersion?)null);

        return payload.Object;
    }

    public static IEnumerable<object[]> UploadMutationErrorCasesWithModes()
    {
        foreach (var errorCase in UploadMutationErrorCases())
        {
            yield return [.. errorCase, InteractionMode.Interactive];
            yield return [.. errorCase, InteractionMode.JsonOutput];
        }
    }

    public static IEnumerable<object[]> UploadMutationErrorCases()
    {
        yield return
        [
            new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_OpenApiCollectionNotFoundError(
                "oa-1", "OpenAPI collection not found."),
            """
            OpenAPI collection not found.
            """
        ];

        yield return
        [
            new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_UnauthorizedOperation(
                "UnauthorizedOperation", "Not authorized to upload."),
            """
            Not authorized to upload.
            """
        ];

        yield return
        [
            new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_DuplicatedTagError(
                "DuplicatedTagError", "Tag 'v1' already exists."),
            """
            Tag 'v1' already exists.
            """
        ];

        yield return
        [
            new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_ConcurrentOperationError(
                "ConcurrentOperationError", "A concurrent operation is in progress."),
            """
            A concurrent operation is in progress.
            """
        ];

        yield return
        [
            new UploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors_InvalidOpenApiCollectionArchiveError(
                "Invalid archive format."),
            """
            The server received an invalid archive. This indicates a bug in the tooling.
            Please notify ChilliCream.
            Error received: Invalid archive format.
            """
        ];

        var unexpectedError = new Mock<IUploadOpenApiCollectionCommandMutation_UploadOpenApiCollection_Errors>();
        unexpectedError
            .As<IError>()
            .SetupGet(x => x.Message)
            .Returns("Something went wrong.");

        yield return
        [
            unexpectedError.Object,
            """
            Unexpected mutation error: Something went wrong.
            """
        ];
    }
}
