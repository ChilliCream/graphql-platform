using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Schemas;

public sealed class PublishSchemaCommandTests(NitroCommandFixture fixture) : SchemasCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Publish a schema version to a stage.

            Usage:
              nitro schema publish [options]

            Options:
              --api-id <api-id> (REQUIRED)  The ID of the API [env: NITRO_API_ID]
              --tag <tag> (REQUIRED)        The tag of the schema version to deploy [env: NITRO_TAG]
              --stage <stage> (REQUIRED)    The name of the stage [env: NITRO_STAGE]
              --force                       Skip confirmation prompts for deletes and overwrites
              --wait-for-approval           Wait for the deployment to be approved before completing [env: NITRO_WAIT_FOR_APPROVAL]
              --cloud-url <cloud-url>       The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>           The API key used for authentication [env: NITRO_API_KEY]
              --output <json>               The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                Show help and usage information

            Example:
              nitro schema publish \
                --api-id "<api-id>" \
                --tag "v1" \
                --stage "dev"
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ForceAndWaitForApproval_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--api-id",
            ApiId,
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--force",
            "--wait-for-approval");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The '--force' and '--wait-for-approval' options are mutually exclusive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoSession_Or_ApiKey_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);
        SetupNoAuthentication();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    [Fact]
    public async Task PublishSchemaThrows_ReturnsError()
    {
        // arrange
        SetupPublishSchemaMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            └── ✕ Failed to publish a new schema version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetPublishSchemaErrors))]
    public async Task PublishSchemaHasErrors_ReturnsError(
        IPublishSchemaVersion_PublishSchema_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupPublishSchemaMutation(errors: error);

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            └── ✕ Failed to publish a new schema version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task MutationReturnsNullRequestId_ReturnsError()
    {
        // arrange
        SchemasClientMock
            .Setup(x => x.StartSchemaPublishAsync(
                ApiId, Stage, Tag, false, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var payload = new Mock<IPublishSchemaVersion_PublishSchema>(MockBehavior.Strict);
                payload.SetupGet(x => x.Errors)
                    .Returns((IReadOnlyList<IPublishSchemaVersion_PublishSchema_Errors>?)null);
                payload.SetupGet(x => x.Id)
                    .Returns((string?)null);
                return payload.Object;
            });

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            └── ✕ Failed to publish a new schema version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The GraphQL mutation completed without errors, but the server did not return the expected data.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task ReturnsSuccess()
    {
        // arrange
        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            ├── Publication request created. (ID: request-id)
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_ReturnsSuccess()
    {
        // arrange
        SetupPublishSchemaMutation(waitForApproval: true);
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            ├── Publication request created. (ID: request-id)
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task BreakingChanges_ReturnsError()
    {
        // arrange
        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            ├── Publication request created. (ID: request-id)
            └── ✕ The new schema version was rejected.
                └── Something went wrong during publish.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The new schema version was rejected.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task BreakingChanges_Force_ReturnsSuccess()
    {
        // arrange
        SetupPublishSchemaMutation(force: true);
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--force");

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            ├── ! Force push is enabled.
            ├── Publication request created. (ID: request-id)
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_BreakingChanges_Approved_ReturnsSuccess()
    {
        // arrange
        SetupPublishSchemaMutation(waitForApproval: true);
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishWaitForApprovalEventWithErrors(),
            CreateSchemaVersionPublishApprovedEvent(),
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--wait-for-approval");

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            ├── Publication request created. (ID: request-id)
            ├── ! Failed validation.
            │   ├── Invalid GraphQL schema
            │   │   └── There is no object type implementing interface `InterfaceWithoutImplementation`. (SCHEMA_INTERFACE_NO_IMPL)
            │   ├── GraphQL schema changes
            │   │   ├── ✕ Directive foo was modified
            │   │   │   ├── ✓ Directive location FieldDefinition added
            │   │   │   └── ✕ Directive location Field removed
            │   │   ├── ✕ Object type Foo was modified
            │   │   │   ├── ✓ Field Foo.bar of type String! was added
            │   │   │   └── ✕ Field Foo.baz of type Int! was removed
            │   │   ├── ! Enum Status was modified
            │   │   │   ├── ! Enum value Status.ACTIVE was added
            │   │   │   └── ✕ Enum value Status.DELETED was removed
            │   │   ├── ✓ Type system member NewType was added.
            │   │   └── ✕ Type system member OldType was removed.
            │   ├── Client 'TestClient' (ID: client-1)
            │   │   └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
            │   │       └── foo (10:10)
            │   ├── OpenAPI collection 'petstore' (ID: collection-1)
            │   │   └── Endpoint 'GET /fail'
            │   │       └── The field `person` does not exist on the type `Query`. (1:14)
            │   ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │   │   └── Tool 'Fail'
            │   │       └── The field `person` does not exist on the type `Query`. (1:14)
            │   ├── There was a syntax error in your schema document.
            │   └── Operations are not allowed in a schema document.
            ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            ├── Your request has been approved.
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    [Fact]
    public async Task WaitForApproval_BreakingChanges_NotApproved_ReturnsError()
    {
        // arrange
        SetupPublishSchemaMutation(waitForApproval: true);
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishWaitForApprovalEventWithErrors(),
            CreateSchemaVersionPublishFailedEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish",
            "--tag",
            Tag,
            "--stage",
            Stage,
            "--api-id",
            ApiId,
            "--wait-for-approval");

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The new schema version was rejected.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            ├── Publication request created. (ID: request-id)
            ├── ! Failed validation.
            │   ├── Invalid GraphQL schema
            │   │   └── There is no object type implementing interface `InterfaceWithoutImplementation`. (SCHEMA_INTERFACE_NO_IMPL)
            │   ├── GraphQL schema changes
            │   │   ├── ✕ Directive foo was modified
            │   │   │   ├── ✓ Directive location FieldDefinition added
            │   │   │   └── ✕ Directive location Field removed
            │   │   ├── ✕ Object type Foo was modified
            │   │   │   ├── ✓ Field Foo.bar of type String! was added
            │   │   │   └── ✕ Field Foo.baz of type Int! was removed
            │   │   ├── ! Enum Status was modified
            │   │   │   ├── ! Enum value Status.ACTIVE was added
            │   │   │   └── ✕ Enum value Status.DELETED was removed
            │   │   ├── ✓ Type system member NewType was added.
            │   │   └── ✕ Type system member OldType was removed.
            │   ├── Client 'TestClient' (ID: client-1)
            │   │   └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
            │   │       └── foo (10:10)
            │   ├── OpenAPI collection 'petstore' (ID: collection-1)
            │   │   └── Endpoint 'GET /fail'
            │   │       └── The field `person` does not exist on the type `Query`. (1:14)
            │   ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
            │   │   └── Tool 'Fail'
            │   │       └── The field `person` does not exist on the type `Query`. (1:14)
            │   ├── There was a syntax error in your schema document.
            │   └── Operations are not allowed in a schema document.
            ├── ⏳ Waiting for approval. Approve in Nitro to continue.
            └── ✕ The new schema version was rejected.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Tag, Tag);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);

        SetupPublishSchemaMutation();
        SetupPublishSchemaSubscription(
            CreateSchemaVersionPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "schema",
            "publish");

        // assert
        result.AssertSuccess(
            """
            Publishing new schema version 'v1' of API 'api-1' to stage 'dev'
            ├── Publication request created. (ID: request-id)
            └── ✓ Published new schema version 'v1' to stage 'dev'.
            """);
    }

    #region Error Theory Data

    public static TheoryData<
        IPublishSchemaVersion_PublishSchema_Errors,
        string> GetPublishSchemaErrors() => new()
    {
        { CreatePublishSchemaUnauthorizedError(), "Unauthorized." },
        { CreatePublishSchemaApiNotFoundError(), $"API '{ApiId}' was not found." },
        { CreatePublishSchemaStageNotFoundError(), $"Stage '{Stage}' was not found." },
        { CreatePublishSchemaSchemaNotFoundError(), "Schema not found." }
    };

    #endregion
}
