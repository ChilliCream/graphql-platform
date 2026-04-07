using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.FusionConfiguration;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionConfigurationPublishCommitCommandTests(NitroCommandFixture fixture)
    : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "publish", "commit", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Commit a Fusion configuration publish.

            Usage:
              nitro fusion publish commit [options]

            Options:
              --request-id <request-id>                            The ID of a request [env: NITRO_REQUEST_ID]
              -a, --archive, --configuration <archive> (REQUIRED)  The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              --cloud-url <cloud-url>                              The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                                  The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                      The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                       Show help and usage information

            Example:
              nitro fusion publish commit --archive ./gateway.far
            """);
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
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run 'nitro login'.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NoRequestId_And_NoStateFile_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupFusionPublishingStateCacheMiss();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            No request ID was provided and no request ID was found in the cache. Please provide a request ID.
            """);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task ArchiveFileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            Archive file '/some/working/directory/fusion.far' does not exist.
            """);
    }

    [Fact]
    public async Task FusionConfigurationUploadThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "commit", "--archive", ArchiveFile, "--request-id", RequestId);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [MemberData(nameof(GetUploadErrors))]
    public async Task FusionConfigurationUploadHasErrors_ReturnsError(
        ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion", "publish", "commit",
            "--request-id", RequestId, "--archive", ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            $"""
             {expectedErrorMessage}
             """);
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Success_CommitsArchive_NonInteractive()
    {
        // arrange
        SetupArchiveFile();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration
            └── ✓ Published Fusion configuration.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Fact]
    public async Task Success_CommitsArchive_Interactive()
    {
        // arrange
        SetupArchiveFile();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();
        SetupInteractionMode(InteractionMode.Interactive);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Fact]
    public async Task Success_CommitsArchive_JsonOutput()
    {
        // arrange
        SetupArchiveFile();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();
        SetupInteractionMode(InteractionMode.JsonOutput);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task RequestIdFromStateFile_Success(InteractionMode mode)
    {
        // arrange
        SetupFusionPublishingStateCache(RequestId);
        SetupArchiveFile();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription();
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--archive",
            ArchiveFile);

        // assert
        Assert.Empty(result.StdErr);
        Assert.Equal(0, result.ExitCode);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    [Fact]
    public async Task Commit_Should_ReturnError_When_CommitFails()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        FusionConfigurationClientMock
            .Setup(x => x.SubscribeToFusionConfigurationPublishingTaskChangedAsync(
                RequestId,
                It.IsAny<CancellationToken>()))
            .Returns(Array.Empty<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged>()
                .ToAsyncEnumerable());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            The commit has failed.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Commit_Should_HandleSubscriptionEvents_When_PublishFails()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreatePublishingFailedEvent(CreatePublishingGenericError("Deployment failed.")));

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.StdOut.MatchInlineSnapshot(
            """
            Publishing Fusion configuration
            └── ✕ Failed to publish a new Fusion configuration version.
                └── Deployment failed.
            """);
        result.StdErr.MatchInlineSnapshot(
            """
            Failed to publish the new configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task Commit_Should_HandleSubscriptionEvents_When_Queued()
    {
        // arrange
        SetupArchiveFile();
        var capturedStream = SetupFusionConfigurationUploadMutation();
        SetupFusionConfigurationUploadSubscription(
            CreateQueuedEvent(2),
            CreatPublishSuccessEvent());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "publish",
            "commit",
            "--request-id",
            RequestId,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Publishing Fusion configuration
            ├── Queued at position 2.
            └── ✓ Published Fusion configuration.
            """);
        var schema = await GetFusionSchemaAsync(capturedStream);
        AssertFusionSchema(schema);
    }

    #region Theory Data

    public static TheoryData<
        ICommitFusionConfigurationPublish_CommitFusionConfigurationPublish_Errors,
        string> GetUploadErrors() => new()
    {
        { CreateUploadUnauthorizedError(), "Unauthorized." },
        { CreateUploadRequestNotFoundError(), "Fusion configuration request was not found." },
        { CreateUploadInvalidStateTransitionError(), "Invalid processing state transition." }
    };

    #endregion

    private void AssertFusionSchema(string schema)
    {
        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query
              @fusion__type(schema: REVIEWS) {
              cachedField: String
                @cacheControl(maxAge: 60, scope: PUBLIC)
                @fusion__field(schema: REVIEWS)
              tag1Field: String
                @fusion__field(schema: REVIEWS)
              tag2Field: String
                @fusion__field(schema: REVIEWS)
            }

            enum CacheControlScope
              @fusion__type(schema: REVIEWS) {
              "The value to cache is specific to a single user."
              PRIVATE
                @fusion__enumValue(schema: REVIEWS)
              "The value to cache is not tied to a single user."
              PUBLIC
                @fusion__enumValue(schema: REVIEWS)
            }

            "The fusion__Schema enum is a generated type used within an execution schema document to refer to a source schema in a type-safe manner."
            enum fusion__Schema {
              REVIEWS
                @fusion__schema_metadata(name: "reviews")
            }

            "The fusion__FieldDefinition scalar is used to represent a GraphQL field definition specified in the GraphQL spec."
            scalar fusion__FieldDefinition

            "The fusion__FieldSelectionMap scalar is used to represent the FieldSelectionMap type specified in the GraphQL Composite Schemas Spec."
            scalar fusion__FieldSelectionMap

            "The fusion__FieldSelectionPath scalar is used to represent a path of field names relative to the Query type."
            scalar fusion__FieldSelectionPath

            "The fusion__FieldSelectionSet scalar is used to represent a GraphQL selection set. To simplify the syntax, the outermost selection set is not wrapped in curly braces."
            scalar fusion__FieldSelectionSet

            directive @cacheControl(inheritMaxAge: Boolean maxAge: Int scope: CacheControlScope sharedMaxAge: Int vary: [String]) on OBJECT | FIELD_DEFINITION | INTERFACE | UNION

            "The @fusion__cost directive specifies cost metadata for each source schema."
            directive @fusion__cost("The name of the source schema that defined the cost metadata." schema: fusion__Schema! "The weight defined in the source schema." weight: String!) repeatable on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            "The @fusion__enumValue directive specifies which source schema provides an enum value."
            directive @fusion__enumValue("The name of the source schema that provides the specified enum value." schema: fusion__Schema!) repeatable on ENUM_VALUE

            "The @fusion__field directive specifies which source schema provides a field in a composite type and what execution behavior it has."
            directive @fusion__field("Indicates that this field is only partially provided and must be combined with `provides`." partial: Boolean! = false "A selection set of fields this field provides in the composite schema." provides: fusion__FieldSelectionSet "The name of the source schema that originally provided this field." schema: fusion__Schema! "The field type in the source schema if it differs in nullability or structure." sourceType: String) repeatable on FIELD_DEFINITION

            "The @fusion__implements directive specifies on which source schema an interface is implemented by an object or interface type."
            directive @fusion__implements("The name of the interface type." interface: String! "The name of the source schema on which the annotated type implements the specified interface." schema: fusion__Schema!) repeatable on OBJECT | INTERFACE

            "The @fusion__inaccessible directive is used to prevent specific type system members from being accessible through the client-facing composite schema, even if they are accessible in the underlying source schemas."
            directive @fusion__inaccessible on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION

            "The @fusion__inputField directive specifies which source schema provides an input field in a composite input type."
            directive @fusion__inputField("The name of the source schema that originally provided this input field." schema: fusion__Schema! "The field type in the source schema if it differs in nullability or structure." sourceType: String) repeatable on ARGUMENT_DEFINITION | INPUT_FIELD_DEFINITION

            "The @fusion__listSize directive specifies list size metadata for each source schema."
            directive @fusion__listSize("The assumed size of the list as defined in the source schema." assumedSize: Int "The single slicing argument requirement of the list as defined in the source schema." requireOneSlicingArgument: Boolean "The name of the source schema that defined the list size metadata." schema: fusion__Schema! "The sized fields of the list as defined in the source schema." sizedFields: [String!] "The slicing argument default value of the list as defined in the source schema." slicingArgumentDefaultValue: Int "The slicing arguments of the list as defined in the source schema." slicingArguments: [String!]) repeatable on FIELD_DEFINITION

            "The @fusion__lookup directive specifies how the distributed executor can resolve data for an entity type from a source schema by a stable key."
            directive @fusion__lookup("The GraphQL field definition in the source schema that can be used to look up the entity." field: fusion__FieldDefinition! "Is the lookup meant as an entry point or just to provide more data." internal: Boolean! = false "A selection set on the annotated entity type that describes the stable key for the lookup." key: fusion__FieldSelectionSet! "The map describes how the key values are resolved from the annotated entity type." map: [fusion__FieldSelectionMap!]! "The path to the lookup field relative to the Query type." path: fusion__FieldSelectionPath "The name of the source schema where the annotated entity type can be looked up from." schema: fusion__Schema!) repeatable on OBJECT | INTERFACE | UNION

            "The @fusion__requires directive specifies if a field has requirements on a source schema."
            directive @fusion__requires("The GraphQL field definition in the source schema that this field depends on." field: fusion__FieldDefinition! "The map describes how the argument values for the source schema are resolved from the arguments of the field exposed in the client-facing composite schema and from required data relative to the current type." map: [fusion__FieldSelectionMap]! "A selection set on the annotated field that describes its requirements." requirements: fusion__FieldSelectionSet! "The name of the source schema where this field has requirements to data on other source schemas." schema: fusion__Schema!) repeatable on FIELD_DEFINITION

            "The @fusion__schema_metadata directive is used to provide additional metadata for a source schema."
            directive @fusion__schema_metadata("The name of the source schema." name: String!) on ENUM_VALUE

            "The @fusion__type directive specifies which source schemas provide parts of a composite type."
            directive @fusion__type("The name of the source schema that originally provided part of the annotated type." schema: fusion__Schema!) repeatable on SCALAR | OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT

            "The @fusion__unionMember directive specifies which source schema provides a member type of a union."
            directive @fusion__unionMember("The name of the member type." member: String! "The name of the source schema that provides the specified member type." schema: fusion__Schema!) repeatable on UNION

            """);
    }
}
