using System.Text;
using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionValidateCommandTests(NitroCommandFixture fixture) : FusionCommandTestBase(fixture)
{
    [Fact]
    public async Task Help_ReturnsSuccess()
    {
        // arrange & act
        var result = await ExecuteCommandAsync("fusion", "validate", "--help");

        // assert
        result.AssertHelpOutput(
            """
            Description:
              Validate a Fusion configuration against a stage.

            Usage:
              nitro fusion validate [options]

            Options:
              --api-id <api-id> (REQUIRED)                   The ID of the API [env: NITRO_API_ID]
              --stage <stage> (REQUIRED)                     The name of the stage [env: NITRO_STAGE]
              -a, --archive, --configuration <archive>       The path to a Fusion archive file (the '--configuration' alias is deprecated) [env: NITRO_FUSION_CONFIG_FILE]
              -f, --source-schema-file <source-schema-file>  One or more paths to a source schema file (.graphqls) or directory containing a source schema file
              --cloud-url <cloud-url>                        The URL of the Nitro backend (only needed for self-hosted or dedicated deployments) [env: NITRO_CLOUD_URL] [default: api.chillicream.com]
              --api-key <api-key>                            The API key used for authentication [env: NITRO_API_KEY]
              --output <json>                                The output format (enables non-interactive mode) [env: NITRO_OUTPUT_FORMAT]
              -?, -h, --help                                 Show help and usage information

            Example:
              nitro fusion validate \
                --api-id "<api-id>" \
                --stage "dev" \
                --source-schema-file ./products/schema.graphqls \
                --source-schema-file ./reviews/schema.graphqls
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
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertError(
            """
            This command requires an authenticated user. Either specify '--api-key' or run `nitro login`.
            """);
    }

    #region Option Validation

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MissingRequiredOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Option '--api-id' is required.
            Option '--stage' is required.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task MultipleExclusiveOptions_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            The options '--source-schema-file' and '--archive' are mutually exclusive.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task NeitherArchiveNorSourceSchemaFile_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Missing one of the required options '--source-schema-file' or '--archive'.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    #region Archive

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithArchive_FileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Archive file '/some/working/directory/fusion.far' does not exist.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_ReturnsSuccess()
    {
        // arrange
        SetupArchiveFile();
        var capturedStream = SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Validation request created. (ID: request-id)
            └── ✓ Schema validation successful.
            """);
        AssertSchemaUpload(capturedStream);
    }

    [Fact]
    public async Task WithArchive_WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);
        SetupEnvironmentVariable(EnvironmentVariables.FusionConfigFile, ArchiveFile);

        SetupArchiveFile();
        var capturedStream = SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate");

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Validation request created. (ID: request-id)
            └── ✓ Schema validation successful.
            """);
        AssertSchemaUpload(capturedStream);
    }

    [Theory]
    [MemberData(nameof(GetValidateSchemaVersionErrors))]
    public async Task WithArchive_ValidateSchemaVersionHasErrors_ReturnsError(
        IValidateSchemaVersion_ValidateSchema_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupArchiveFile();
        SetupSchemaValidationMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_ValidateSchemaVersionThrows_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupSchemaValidationMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithArchive_BreakingChanges_ReturnsError()
    {
        // arrange
        SetupArchiveFile();
        SetupFusionConfigurationDownload();
        SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription(
            CreateSchemaVersionOperationInProgressEvent(),
            CreateSchemaVersionValidationInProgressEvent(),
            CreateSchemaVersionValidationFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--archive",
            ArchiveFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Fusion configuration failed validation.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Validation request created. (ID: request-id)
            ├── Validating...
            ├── Validating...
            └── ✕ Schema failed validation.
                ├── GraphQL schema changes
                │   ├── ✕ Directive foo was modified
                │   │   ├── ✓ Directive location FieldDefinition added
                │   │   └── ✕ Directive location Field removed
                │   ├── ✕ Object type Foo was modified
                │   │   ├── ✓ Field Foo.bar of type String! was added
                │   │   └── ✕ Field Foo.baz of type Int! was removed
                │   ├── ! Enum Status was modified
                │   │   ├── ! Enum value Status.ACTIVE was added
                │   │   └── ✕ Enum value Status.DELETED was removed
                │   ├── ✓ Type system member NewType was added.
                │   └── ✕ Type system member OldType was removed.
                ├── Invalid GraphQL schema
                │   └── There is no object type implementing interface `InterfaceWithoutImplementation`. (SCHEMA_INTERFACE_NO_IMPL)
                ├── Client 'TestClient' (ID: client-1)
                │   └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
                │       └── foo (10:10)
                ├── OpenAPI collection 'petstore' (ID: collection-1)
                │   └── Endpoint 'GET /fail'
                │       └── The field `person` does not exist on the type `Query`. (1:14)
                ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
                │   └── Tool 'Fail'
                │       └── The field `person` does not exist on the type `Query`. (1:14)
                └── An unexpected error occurred.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    #region Source Schema File

    [Theory]
    [InlineData(InteractionMode.Interactive)]
    [InlineData(InteractionMode.NonInteractive)]
    [InlineData(InteractionMode.JsonOutput)]
    public async Task WithSourceSchemaFile_FileDoesNotExist_ReturnsError(InteractionMode mode)
    {
        // arrange
        SetupInteractionMode(mode);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Schema file '/some/working/directory/products/schema.graphqls' does not exist.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ReturnsSuccess()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        var capturedStream = SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            ├── Validation request created. (ID: request-id)
            └── ✓ Schema validation successful.
            """);
        AssertSchemaUploadAfterCompose(capturedStream);
    }

    [Fact]
    public async Task WithSourceSchemaFile_WithEnvVars_ReturnsSuccess()
    {
        // arrange
        SetupEnvironmentVariable(EnvironmentVariables.ApiId, ApiId);
        SetupEnvironmentVariable(EnvironmentVariables.Stage, Stage);

        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        var capturedStream = SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.AssertSuccess(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            ├── Validation request created. (ID: request-id)
            └── ✓ Schema validation successful.
            """);
        AssertSchemaUploadAfterCompose(capturedStream);
    }

    [Theory]
    [MemberData(nameof(GetValidateSchemaVersionErrors))]
    public async Task WithSourceSchemaFile_ValidateSchemaVersionHasErrors_ReturnsError(
        IValidateSchemaVersion_ValidateSchema_Errors error,
        string expectedErrorMessage)
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        SetupSchemaValidationMutation(error);

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(expectedErrorMessage);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ValidateSchemaVersionThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        SetupSchemaValidationMutationException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_BreakingChanges_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownload();
        SetupSchemaValidationMutation();
        SetupSchemaValidationSubscription(
            CreateSchemaVersionOperationInProgressEvent(),
            CreateSchemaVersionValidationInProgressEvent(),
            CreateSchemaVersionValidationFailedEventWithErrors());

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Fusion configuration failed validation.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✓ Composed new configuration.
            ├── Validation request created. (ID: request-id)
            ├── Validating...
            ├── Validating...
            └── ✕ Schema failed validation.
                ├── GraphQL schema changes
                │   ├── ✕ Directive foo was modified
                │   │   ├── ✓ Directive location FieldDefinition added
                │   │   └── ✕ Directive location Field removed
                │   ├── ✕ Object type Foo was modified
                │   │   ├── ✓ Field Foo.bar of type String! was added
                │   │   └── ✕ Field Foo.baz of type Int! was removed
                │   ├── ! Enum Status was modified
                │   │   ├── ! Enum value Status.ACTIVE was added
                │   │   └── ✕ Enum value Status.DELETED was removed
                │   ├── ✓ Type system member NewType was added.
                │   └── ✕ Type system member OldType was removed.
                ├── Invalid GraphQL schema
                │   └── There is no object type implementing interface `InterfaceWithoutImplementation`. (SCHEMA_INTERFACE_NO_IMPL)
                ├── Client 'TestClient' (ID: client-1)
                │   └── Operation '6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59' (Deployed tags: 1.0.0)
                │       └── foo (10:10)
                ├── OpenAPI collection 'petstore' (ID: collection-1)
                │   └── Endpoint 'GET /fail'
                │       └── The field `person` does not exist on the type `Query`. (1:14)
                ├── MCP Feature Collection 'mcp-collection' (ID: mcp-1)
                │   └── Tool 'Fail'
                │       └── The field `person` does not exist on the type `Query`. (1:14)
                └── An unexpected error occurred.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_ConfigurationDownloadThrows_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFile();
        SetupFusionConfigurationDownloadException();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            There was an unexpected error: Something unexpected happened.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Downloading existing Fusion configuration
            │   └── ✕ Failed to download existing Fusion configuration.
            └── ✕ Failed to validate the Fusion configuration.
            """);
        Assert.Equal(1, result.ExitCode);
    }

    [Fact]
    public async Task WithSourceSchemaFile_CompositionErrors_ReturnsError()
    {
        // arrange
        SetupSourceSchemaFileWithInvalidSchema();
        SetupFusionConfigurationDownload();

        // act
        var result = await ExecuteCommandAsync(
            "fusion",
            "validate",
            "--api-id",
            ApiId,
            "--stage",
            Stage,
            "--source-schema-file",
            SourceSchemaFile);

        // assert
        result.StdErr.MatchInlineSnapshot(
            """
            Source schema validation failed.
            """);
        result.StdOut.MatchInlineSnapshot(
            """
            Validating Fusion configuration of API 'api-1' against stage 'dev'
            ├── Downloading existing Fusion configuration
            │   └── ✓ Downloaded existing configuration from 'dev'.
            ├── Composing new Fusion configuration
            │   └── ✕ The Fusion configuration could not be composed.
            └── ✕ Failed to validate the Fusion configuration.

            ## Composition log

            ❌ [ERR] The @require directive on argument 'Query.field(arg:)' in schema 'products' contains invalid syntax in the 'field' argument. (REQUIRE_INVALID_SYNTAX)
            """);
        Assert.Equal(1, result.ExitCode);
    }

    #endregion

    private static void AssertSchemaUpload(MemoryStream stream)
    {
        var str = Encoding.UTF8.GetString(stream.ToArray());
        str.MatchInlineSnapshot(
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

    private static void AssertSchemaUploadAfterCompose(MemoryStream stream)
    {
        var str = Encoding.UTF8.GetString(stream.ToArray());
        str.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: PRODUCTS) @fusion__type(schema: REVIEWS) {
              cachedField: String
                @cacheControl(maxAge: 60, scope: PUBLIC)
                @fusion__field(schema: REVIEWS)
              field: String! @fusion__field(schema: PRODUCTS)
              tag1Field: String @fusion__field(schema: REVIEWS)
              tag2Field: String @fusion__field(schema: REVIEWS)
            }

            enum CacheControlScope @fusion__type(schema: REVIEWS) {
              "The value to cache is specific to a single user."
              PRIVATE @fusion__enumValue(schema: REVIEWS)
              "The value to cache is not tied to a single user."
              PUBLIC @fusion__enumValue(schema: REVIEWS)
            }

            "The fusion__Schema enum is a generated type used within an execution schema document to refer to a source schema in a type-safe manner."
            enum fusion__Schema {
              PRODUCTS @fusion__schema_metadata(name: "products")
              REVIEWS @fusion__schema_metadata(name: "reviews")
            }

            "The fusion__FieldDefinition scalar is used to represent a GraphQL field definition specified in the GraphQL spec."
            scalar fusion__FieldDefinition

            "The fusion__FieldSelectionMap scalar is used to represent the FieldSelectionMap type specified in the GraphQL Composite Schemas Spec."
            scalar fusion__FieldSelectionMap

            "The fusion__FieldSelectionPath scalar is used to represent a path of field names relative to the Query type."
            scalar fusion__FieldSelectionPath

            "The fusion__FieldSelectionSet scalar is used to represent a GraphQL selection set. To simplify the syntax, the outermost selection set is not wrapped in curly braces."
            scalar fusion__FieldSelectionSet

            directive @cacheControl(
              inheritMaxAge: Boolean
              maxAge: Int
              scope: CacheControlScope
              sharedMaxAge: Int
              vary: [String]
            ) on OBJECT | FIELD_DEFINITION | INTERFACE | UNION

            "The @fusion__cost directive specifies cost metadata for each source schema."
            directive @fusion__cost(
              "The name of the source schema that defined the cost metadata."
              schema: fusion__Schema!
              "The weight defined in the source schema."
              weight: String!
            ) repeatable on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | ENUM | INPUT_FIELD_DEFINITION

            "The @fusion__enumValue directive specifies which source schema provides an enum value."
            directive @fusion__enumValue(
              "The name of the source schema that provides the specified enum value."
              schema: fusion__Schema!
            ) repeatable on ENUM_VALUE

            "The @fusion__field directive specifies which source schema provides a field in a composite type and what execution behavior it has."
            directive @fusion__field(
              "Indicates that this field is only partially provided and must be combined with `provides`."
              partial: Boolean! = false
              "A selection set of fields this field provides in the composite schema."
              provides: fusion__FieldSelectionSet
              "The name of the source schema that originally provided this field."
              schema: fusion__Schema!
              "The field type in the source schema if it differs in nullability or structure."
              sourceType: String
            ) repeatable on FIELD_DEFINITION

            "The @fusion__implements directive specifies on which source schema an interface is implemented by an object or interface type."
            directive @fusion__implements(
              "The name of the interface type."
              interface: String!
              "The name of the source schema on which the annotated type implements the specified interface."
              schema: fusion__Schema!
            ) repeatable on OBJECT | INTERFACE

            "The @fusion__inaccessible directive is used to prevent specific type system members from being accessible through the client-facing composite schema, even if they are accessible in the underlying source schemas."
            directive @fusion__inaccessible on SCALAR | OBJECT | FIELD_DEFINITION | ARGUMENT_DEFINITION | INTERFACE | UNION | ENUM | ENUM_VALUE | INPUT_OBJECT | INPUT_FIELD_DEFINITION

            "The @fusion__inputField directive specifies which source schema provides an input field in a composite input type."
            directive @fusion__inputField(
              "The name of the source schema that originally provided this input field."
              schema: fusion__Schema!
              "The field type in the source schema if it differs in nullability or structure."
              sourceType: String
            ) repeatable on ARGUMENT_DEFINITION | INPUT_FIELD_DEFINITION

            "The @fusion__listSize directive specifies list size metadata for each source schema."
            directive @fusion__listSize(
              "The assumed size of the list as defined in the source schema."
              assumedSize: Int
              "The single slicing argument requirement of the list as defined in the source schema."
              requireOneSlicingArgument: Boolean
              "The name of the source schema that defined the list size metadata."
              schema: fusion__Schema!
              "The sized fields of the list as defined in the source schema."
              sizedFields: [String!]
              "The slicing argument default value of the list as defined in the source schema."
              slicingArgumentDefaultValue: Int
              "The slicing arguments of the list as defined in the source schema."
              slicingArguments: [String!]
            ) repeatable on FIELD_DEFINITION

            "The @fusion__lookup directive specifies how the distributed executor can resolve data for an entity type from a source schema by a stable key."
            directive @fusion__lookup(
              "The GraphQL field definition in the source schema that can be used to look up the entity."
              field: fusion__FieldDefinition!
              "Is the lookup meant as an entry point or just to provide more data."
              internal: Boolean! = false
              "A selection set on the annotated entity type that describes the stable key for the lookup."
              key: fusion__FieldSelectionSet!
              "The map describes how the key values are resolved from the annotated entity type."
              map: [fusion__FieldSelectionMap!]!
              "The path to the lookup field relative to the Query type."
              path: fusion__FieldSelectionPath
              "The name of the source schema where the annotated entity type can be looked up from."
              schema: fusion__Schema!
            ) repeatable on OBJECT | INTERFACE | UNION

            "The @fusion__requires directive specifies if a field has requirements on a source schema."
            directive @fusion__requires(
              "The GraphQL field definition in the source schema that this field depends on."
              field: fusion__FieldDefinition!
              "The map describes how the argument values for the source schema are resolved from the arguments of the field exposed in the client-facing composite schema and from required data relative to the current type."
              map: [fusion__FieldSelectionMap]!
              "A selection set on the annotated field that describes its requirements."
              requirements: fusion__FieldSelectionSet!
              "The name of the source schema where this field has requirements to data on other source schemas."
              schema: fusion__Schema!
            ) repeatable on FIELD_DEFINITION

            "The @fusion__schema_metadata directive is used to provide additional metadata for a source schema."
            directive @fusion__schema_metadata(
              "The name of the source schema."
              name: String!
            ) on ENUM_VALUE

            "The @fusion__type directive specifies which source schemas provide parts of a composite type."
            directive @fusion__type(
              "The name of the source schema that originally provided part of the annotated type."
              schema: fusion__Schema!
            ) repeatable on SCALAR | OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT

            "The @fusion__unionMember directive specifies which source schema provides a member type of a union."
            directive @fusion__unionMember(
              "The name of the member type."
              member: String!
              "The name of the source schema that provides the specified member type."
              schema: fusion__Schema!
            ) repeatable on UNION

            """);
    }

    #region Error Theory Data

    public static TheoryData<
        IValidateSchemaVersion_ValidateSchema_Errors,
        string> GetValidateSchemaVersionErrors() => new()
    {
        { CreateValidateSchemaVersionUnauthorizedError(), "Unauthorized." },
        { CreateValidateSchemaVersionApiNotFoundError(), $"API '{ApiId}' was not found." },
        { CreateValidateSchemaVersionStageNotFoundError(), $"Stage '{Stage}' was not found." },
        { CreateValidateSchemaVersionSchemaNotFoundError(), "Schema not found." }
    };

    #endregion
}
