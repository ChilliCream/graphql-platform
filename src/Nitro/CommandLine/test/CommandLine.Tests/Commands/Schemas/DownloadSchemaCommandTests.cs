using ChilliCream.Nitro.Client.Exceptions;
using ChilliCream.Nitro.Client.Schemas;
using ChilliCream.Nitro.CommandLine.Helpers;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class DownloadSchemaCommandTests
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddArguments(
                "schema",
                "download",
                "--help")
            .ExecuteAsync();

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Download a schema from a stage

            Usage:
              nitro schema download [options]

            Options:
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --file <file> (REQUIRED)      The file where the schema is stored
              --cloud-url <cloud-url>       The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>               The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
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
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange & act
        var result = await new CommandBuilder()
            .AddApiKey()
            .AddInteractionMode(mode)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production")
            .ExecuteAsync();

        // assert
        var output = result.StdOut.Replace(result.ExecutableName, "nitro");
        output.MatchInlineSnapshot(
            """
            Description:
              Download a schema from a stage

            Usage:
              nitro schema download [options]

            Options:
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --file <file> (REQUIRED)      The file where the schema is stored
              --cloud-url <cloud-url>       The URL of the API. [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key that is used for the authentication [env: NITRO_API_KEY]
              --output <json>               The format in which the result should be displayed, if this option is set, the console will be non-interactive and the result will be displayed in the specified format [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--file' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientException("download failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching Schema...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: download failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientException("download failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Fetching Schema...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error executing your request: download failed
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientException("download failed"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            There was an unexpected error executing your request: download failed
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_NonInteractive()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching Schema...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_Interactive()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Fetching Schema...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task ClientThrowsAuthorizationException_ReturnsError_JsonOutput()
    {
        // arrange
        var client = CreateDownloadExceptionClient(new NitroClientAuthorizationException("forbidden"));

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            The server rejected your request as unauthorized. Ensure your account or API key has the proper permissions for this action.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task SchemaNotFound_ReturnsError_NonInteractive()
    {
        // arrange
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching Schema...
            └── ✕ Failed!
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find a published schema on stage 'production'.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task SchemaNotFound_ReturnsError_Interactive()
    {
        // arrange
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Fetching Schema...
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Could not find a published schema on stage 'production'.
            """);
        Assert.Equal(1, result.ExitCode);

        client.VerifyAll();
    }

    [Fact]
    public async Task SchemaNotFound_ReturnsError_JsonOutput()
    {
        // arrange
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream?)null);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.AssertError(
            """
            Could not find a published schema on stage 'production'.
            """);

        client.VerifyAll();
    }

    [Fact]
    public async Task Success_DownloadsSchema_NonInteractive()
    {
        // arrange
        var schemaContent = "type Query { hello: String }"u8.ToArray();
        var schemaStream = new MemoryStream(schemaContent);
        var fileStream = new MemoryStream();

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(schemaStream);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("schema.graphql")).Returns(false);
        fileSystem.Setup(x => x.CreateFile("schema.graphql")).Returns(fileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching Schema...
            └── ✓ Downloaded schema to 'schema.graphql'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(schemaContent, fileStream.ToArray());

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_DownloadsSchema_Interactive()
    {
        // arrange
        var schemaContent = "type Query { hello: String }"u8.ToArray();
        var schemaStream = new MemoryStream(schemaContent);
        var fileStream = new MemoryStream();

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(schemaStream);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("schema.graphql")).Returns(false);
        fileSystem.Setup(x => x.CreateFile("schema.graphql")).Returns(fileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.Interactive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """

            [    ] Downloaded schema to 'schema.graphql'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(schemaContent, fileStream.ToArray());

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_DownloadsSchema_JsonOutput()
    {
        // arrange
        var schemaContent = "type Query { hello: String }"u8.ToArray();
        var schemaStream = new MemoryStream(schemaContent);
        var fileStream = new MemoryStream();

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(schemaStream);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("schema.graphql")).Returns(false);
        fileSystem.Setup(x => x.CreateFile("schema.graphql")).Returns(fileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.JsonOutput)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            {
              "file": "schema.graphql"
            }
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(schemaContent, fileStream.ToArray());

        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    [Fact]
    public async Task Success_DeletesExistingFile_BeforeDownload()
    {
        // arrange
        var schemaStream = new MemoryStream("schema"u8.ToArray());
        var fileStream = new MemoryStream();

        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(schemaStream);

        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(x => x.FileExists("schema.graphql")).Returns(true);
        fileSystem.Setup(x => x.DeleteFile("schema.graphql"));
        fileSystem.Setup(x => x.CreateFile("schema.graphql")).Returns(fileStream);

        // act
        var result = await new CommandBuilder()
            .AddService(client.Object)
            .AddService(fileSystem.Object)
            .AddApiKey()
            .AddInteractionMode(InteractionMode.NonInteractive)
            .AddArguments(
                "schema",
                "download",
                "--api-id",
                "api-1",
                "--stage",
                "production",
                "--file",
                "schema.graphql")
            .ExecuteAsync();

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Fetching Schema...
            └── ✓ Downloaded schema to 'schema.graphql'.
            """);
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);

        fileSystem.Verify(x => x.DeleteFile("schema.graphql"), Times.Once);
        client.VerifyAll();
        fileSystem.VerifyAll();
    }

    private static Mock<ISchemasClient> CreateDownloadExceptionClient(Exception ex)
    {
        var client = new Mock<ISchemasClient>(MockBehavior.Strict);
        client.Setup(x => x.DownloadLatestSchemaAsync(
                "api-1",
                "production",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return client;
    }
}
