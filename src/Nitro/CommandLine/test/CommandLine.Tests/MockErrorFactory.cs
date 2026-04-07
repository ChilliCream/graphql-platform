using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests;

internal static class MockErrorFactory
{
    #region Individual Error Setup Methods

    public static void SetupInvalidGraphQLSchemaError<T>(Mock<T> mock) where T : class
    {
        var schemaErrorEntry = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Errors>(
            MockBehavior.Strict);
        schemaErrorEntry.SetupGet(x => x.Message)
            .Returns("There is no object type implementing interface `InterfaceWithoutImplementation`.");
        schemaErrorEntry.SetupGet(x => x.Code).Returns("SCHEMA_INTERFACE_NO_IMPL");

        mock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.__typename)
            .Returns("InvalidGraphQLSchemaError");
        mock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.Message)
            .Returns("The schema document contains logical errors and does not comply with the GraphQL specification.");
        mock.As<IInvalidGraphQLSchemaError>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { schemaErrorEntry.Object });
    }

    public static void SetupOperationsAreNotAllowedError<T>(Mock<T> mock) where T : class
    {
        mock.As<IOperationsAreNotAllowedError>()
            .SetupGet(x => x.__typename)
            .Returns("OperationsAreNotAllowedError");
        mock.As<IOperationsAreNotAllowedError>()
            .SetupGet(x => x.Message)
            .Returns("Operations are not allowed in a schema document.");
    }

    public static void SetupSchemaVersionSyntaxError<T>(Mock<T> mock) where T : class
    {
        mock.As<ISchemaVersionSyntaxError>()
            .SetupGet(x => x.__typename)
            .Returns("SchemaVersionSyntaxError");
        mock.As<ISchemaVersionSyntaxError>()
            .SetupGet(x => x.Message)
            .Returns("There was a syntax error in your schema document.");
        mock.As<ISchemaVersionSyntaxError>()
            .SetupGet(x => x.Column)
            .Returns(1);
        mock.As<ISchemaVersionSyntaxError>()
            .SetupGet(x => x.Position)
            .Returns(1);
        mock.As<ISchemaVersionSyntaxError>()
            .SetupGet(x => x.Line)
            .Returns(1);
    }

    public static void SetupPersistedQueryValidationError<T>(Mock<T> mock) where T : class
    {
        var location = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors_Locations>(
            MockBehavior.Strict);
        location.SetupGet(x => x.Line).Returns(10);
        location.SetupGet(x => x.Column).Returns(10);

        var queryError = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors>(
            MockBehavior.Strict);
        queryError.SetupGet(x => x.Message).Returns("foo");
        queryError.SetupGet(x => x.Code).Returns("bar");
        queryError.SetupGet(x => x.Path).Returns("asd");
        queryError.SetupGet(x => x.Locations).Returns(new[] { location.Object });

        var pqClient = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Client>(
            MockBehavior.Strict);
        pqClient.SetupGet(x => x.Name).Returns("TestClient");
        pqClient.SetupGet(x => x.Id).Returns("client-1");

        var pqQuery = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries>(
            MockBehavior.Strict);
        pqQuery.SetupGet(x => x.Hash)
            .Returns("6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59");
        pqQuery.SetupGet(x => x.Message).Returns("def");
        pqQuery.SetupGet(x => x.DeployedTags).Returns(new[] { "1.0.0" });
        pqQuery.SetupGet(x => x.Errors).Returns(new[] { queryError.Object });

        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Message)
            .Returns("There were persisted queries that failed");
        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Client)
            .Returns(pqClient.Object);
        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Queries)
            .Returns(new[] { pqQuery.Object });
    }

    public static void SetupClientPersistedQueryValidationError<T>(Mock<T> mock) where T : class
    {
        var location = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors_Locations>(
            MockBehavior.Strict);
        location.SetupGet(x => x.Line).Returns(10);
        location.SetupGet(x => x.Column).Returns(10);

        var queryError = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries_Errors>(
            MockBehavior.Strict);
        queryError.SetupGet(x => x.Message).Returns("foo");
        queryError.SetupGet(x => x.Code).Returns("bar");
        queryError.SetupGet(x => x.Path).Returns("asd");
        queryError.SetupGet(x => x.Locations).Returns(new[] { location.Object });

        var pqQuery = new Mock<
            IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Queries>(
            MockBehavior.Strict);
        pqQuery.SetupGet(x => x.Hash)
            .Returns("6D12E4A815C50C504695E548EAF680BC8F337AC87E763E5689C685522A01BC59");
        pqQuery.SetupGet(x => x.Message).Returns("def");
        pqQuery.SetupGet(x => x.DeployedTags).Returns(new[] { "1.0.0" });
        pqQuery.SetupGet(x => x.Errors).Returns(new[] { queryError.Object });

        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Message)
            .Returns("There were persisted queries that failed");
        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Client)
            .Returns((IOnClientVersionValidationUpdated_OnClientVersionValidationUpdate_Errors_Client?)null);
        mock.As<IPersistedQueryValidationError>()
            .SetupGet(x => x.Queries)
            .Returns(new[] { pqQuery.Object });
    }

    public static void SetupOpenApiCollectionValidationError<T>(Mock<T> mock) where T : class
    {
        var location = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations_1>(
            MockBehavior.Strict);
        location.SetupGet(x => x.Line).Returns(1);
        location.SetupGet(x => x.Column).Returns(14);

        var docError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_OpenApiCollectionValidationDocumentError>(
            MockBehavior.Strict);
        docError.SetupGet(x => x.Code).Returns((string?)null);
        docError.SetupGet(x => x.Message).Returns("The field `person` does not exist on the type `Query`.");
        docError.SetupGet(x => x.Path).Returns((string?)null);
        docError.SetupGet(x => x.Locations).Returns(new[] { location.Object });

        var endpoint = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_OpenApiCollectionValidationEndpoint>(
            MockBehavior.Strict);
        endpoint.As<IOpenApiCollectionValidationEndpoint>().SetupGet(x => x.HttpMethod).Returns("GET");
        endpoint.As<IOpenApiCollectionValidationEndpoint>().SetupGet(x => x.Route).Returns("/fail");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_1[]
            entityErrors = [docError.Object];
        endpoint.As<IOpenApiCollectionValidationEntity_OpenApiCollectionValidationEndpoint>()
            .SetupGet(x => x.Errors).Returns(entityErrors);
        endpoint.As<IOpenApiCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(entityErrors);

        var openApiCol = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_OpenApiCollection>(
            MockBehavior.Strict);
        openApiCol.SetupGet(x => x.Name).Returns("petstore");
        openApiCol.SetupGet(x => x.Id).Returns("collection-1");

        var colValidation = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_OpenApiCollectionValidationCollection>(
            MockBehavior.Strict);
        colValidation.SetupGet(x => x.OpenApiCollection).Returns(openApiCol.Object);
        colValidation.SetupGet(x => x.Entities).Returns(new[] { endpoint.Object });

        mock.As<IOpenApiCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_1[]
                { colValidation.Object });
    }

    public static void SetupMcpFeatureCollectionValidationError<T>(Mock<T> mock) where T : class
    {
        SetupMcpFeatureCollectionValidationError(mock, "The field `person` does not exist on the type `Query`.");
    }

    public static void SetupMcpFeatureCollectionValidationError<T>(
        Mock<T> mock,
        string docErrorMessage) where T : class
    {
        var location = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_Locations>(
            MockBehavior.Strict);
        location.SetupGet(x => x.Line).Returns(1);
        location.SetupGet(x => x.Column).Returns(14);

        var docError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors_McpFeatureCollectionValidationDocumentError>(
            MockBehavior.Strict);
        docError.SetupGet(x => x.Code).Returns((string?)null);
        docError.SetupGet(x => x.Message).Returns(docErrorMessage);
        docError.SetupGet(x => x.Path).Returns((string?)null);
        docError.SetupGet(x => x.Locations).Returns(new[] { location.Object });

        var tool = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_McpFeatureCollectionValidationTool>(
            MockBehavior.Strict);
        tool.As<IMcpFeatureCollectionValidationTool>().SetupGet(x => x.Name).Returns("Fail");
        IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_Entities_Errors[]
            entityErrors = [docError.Object];
        tool.As<IMcpFeatureCollectionValidationEntity_McpFeatureCollectionValidationTool>()
            .SetupGet(x => x.Errors).Returns(entityErrors);
        tool.As<IMcpFeatureCollectionValidationEntity>().SetupGet(x => x.Errors).Returns(entityErrors);

        var mcpCol = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollection>(
            MockBehavior.Strict);
        mcpCol.SetupGet(x => x.Name).Returns("mcp-collection");
        mcpCol.SetupGet(x => x.Id).Returns("mcp-1");

        var colValidation = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections_McpFeatureCollectionValidationCollection>(
            MockBehavior.Strict);
        colValidation.SetupGet(x => x.McpFeatureCollection).Returns(mcpCol.Object);
        colValidation.SetupGet(x => x.Entities).Returns(new[] { tool.Object });

        mock.As<IMcpFeatureCollectionValidationError>()
            .SetupGet(x => x.Collections)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Collections[]
                { colValidation.Object });
    }

    public static void SetupSchemaChangeViolationError<T>(Mock<T> mock) where T : class
    {
        var changes = CreateSchemaChangeMocks();

        // Set up for validation contexts (ISchemaVersionChangeViolationError)
        mock.As<ISchemaVersionChangeViolationError>()
            .SetupGet(x => x.__typename)
            .Returns("SchemaVersionChangeViolationError");
        mock.As<ISchemaVersionChangeViolationError>()
            .SetupGet(x => x.Changes)
            .Returns(changes
                .Select(c =>
                    (IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes)
                    c.Object)
                .ToArray());

        // Set up for deployment contexts (ISchemaChangeViolationError)
        mock.As<ISchemaChangeViolationError>()
            .SetupGet(x => x.Message)
            .Returns("Schema change violations detected.");
        mock.As<ISchemaChangeViolationError>()
            .SetupGet(x => x.Changes)
            .Returns(changes
                .Select(c =>
                    (IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes)
                    c.Object)
                .ToArray());
    }

    public static void SetupUnexpectedProcessingError<T>(
        Mock<T> mock,
        string message = "An unexpected error occurred.") where T : class
    {
        mock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.__typename)
            .Returns("UnexpectedProcessingError");
        mock.As<IUnexpectedProcessingError>()
            .SetupGet(x => x.Message)
            .Returns(message);
    }

    #endregion

    #region Schema Change Mock Helpers

    private static List<Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>>
        CreateSchemaChangeMocks()
    {
        var changes = new List<Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>>();

        // 1. DirectiveModifiedChange with sub-changes
        changes.Add(CreateDirectiveModifiedChange());

        // 2. ObjectModifiedChange with sub-changes
        changes.Add(CreateObjectModifiedChange());

        // 3. EnumModifiedChange with sub-changes
        changes.Add(CreateEnumModifiedChange());

        // 4. TypeSystemMemberAddedChange
        changes.Add(CreateTypeSystemMemberAddedChange());

        // 5. TypeSystemMemberRemovedChange
        changes.Add(CreateTypeSystemMemberRemovedChange());

        return changes;
    }

    private static Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>
        CreateDirectiveModifiedChange()
    {
        // Sub-change: DirectiveLocationAdded (FIELD_DEFINITION, Safe)
        var locationAdded = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes>(
            MockBehavior.Strict);
        locationAdded
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes_Changes>();
        locationAdded.As<IDirectiveLocationAdded>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Safe);
        locationAdded.As<IDirectiveLocationAdded>()
            .SetupGet(x => x.Location).Returns(DirectiveLocation.FieldDefinition);
        locationAdded.As<IDirectiveLocationAdded>()
            .SetupGet(x => x.__typename).Returns("DirectiveLocationAdded");
        locationAdded.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Safe);

        // Sub-change: DirectiveLocationRemoved (FIELD, Breaking)
        var locationRemoved = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes>(
            MockBehavior.Strict);
        locationRemoved
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes_Changes>();
        locationRemoved.As<IDirectiveLocationRemoved>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        locationRemoved.As<IDirectiveLocationRemoved>()
            .SetupGet(x => x.Location).Returns(DirectiveLocation.Field);
        locationRemoved.As<IDirectiveLocationRemoved>()
            .SetupGet(x => x.__typename).Returns("DirectiveLocationRemoved");
        locationRemoved.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);

        var subChanges = new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes[]
        {
            locationAdded.Object, locationRemoved.Object
        };

        var change = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>(
            MockBehavior.Strict);
        change
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes>();
        change.As<IDirectiveModifiedChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        change.As<IDirectiveModifiedChange>()
            .SetupGet(x => x.Coordinate).Returns("foo");
        change.As<IDirectiveModifiedChange>()
            .SetupGet(x => x.__typename).Returns("DirectiveModifiedChange");
        change.As<IDirectiveModifiedChange>()
            .SetupGet(x => x.Changes).Returns(subChanges);
        change.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        change.SetupGet(x => x.__typename).Returns("DirectiveModifiedChange");

        return change;
    }

    private static Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>
        CreateObjectModifiedChange()
    {
        // Sub-change: FieldAddedChange (Foo.bar, type String!, Safe)
        var fieldAdded = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_4>(
            MockBehavior.Strict);
        fieldAdded
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes_Changes_4>();
        fieldAdded.As<IFieldAddedChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Safe);
        fieldAdded.As<IFieldAddedChange>()
            .SetupGet(x => x.Coordinate).Returns("Foo.bar");
        fieldAdded.As<IFieldAddedChange>()
            .SetupGet(x => x.TypeName).Returns("String!");
        fieldAdded.As<IFieldAddedChange>()
            .SetupGet(x => x.FieldName).Returns("bar");
        fieldAdded.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Safe);

        // Sub-change: FieldRemovedChange (Foo.baz, type Int!, Breaking)
        var fieldRemoved = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_4>(
            MockBehavior.Strict);
        fieldRemoved
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes_Changes_4>();
        fieldRemoved.As<IFieldRemovedChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        fieldRemoved.As<IFieldRemovedChange>()
            .SetupGet(x => x.Coordinate).Returns("Foo.baz");
        fieldRemoved.As<IFieldRemovedChange>()
            .SetupGet(x => x.TypeName).Returns("Int!");
        fieldRemoved.As<IFieldRemovedChange>()
            .SetupGet(x => x.FieldName).Returns("baz");
        fieldRemoved.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);

        var subChanges = new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_4[]
        {
            fieldAdded.Object, fieldRemoved.Object
        };

        var change = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>(
            MockBehavior.Strict);
        change
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes>();
        change.As<IObjectModifiedChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        change.As<IObjectModifiedChange>()
            .SetupGet(x => x.Coordinate).Returns("Foo");
        change.As<IObjectModifiedChange>()
            .SetupGet(x => x.__typename).Returns("ObjectModifiedChange");
        change.As<IObjectModifiedChange>()
            .SetupGet(x => x.Changes).Returns(subChanges);
        change.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        change.SetupGet(x => x.__typename).Returns("ObjectModifiedChange");

        return change;
    }

    private static Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>
        CreateEnumModifiedChange()
    {
        // Sub-change: EnumValueAdded (Status.ACTIVE, Dangerous)
        var enumAdded = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_1>(
            MockBehavior.Strict);
        enumAdded
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes_Changes_1>();
        enumAdded.As<IEnumValueAdded>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Dangerous);
        enumAdded.As<IEnumValueAdded>()
            .SetupGet(x => x.Coordinate).Returns("Status.ACTIVE");
        enumAdded.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Dangerous);

        // Sub-change: EnumValueRemoved (Status.DELETED, Breaking)
        var enumRemoved = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_1>(
            MockBehavior.Strict);
        enumRemoved
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes_Changes_1>();
        enumRemoved.As<IEnumValueRemoved>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        enumRemoved.As<IEnumValueRemoved>()
            .SetupGet(x => x.Coordinate).Returns("Status.DELETED");
        enumRemoved.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);

        var subChanges = new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes_Changes_1[]
        {
            enumAdded.Object, enumRemoved.Object
        };

        var change = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>(
            MockBehavior.Strict);
        change
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes>();
        change.As<IEnumModifiedChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Dangerous);
        change.As<IEnumModifiedChange>()
            .SetupGet(x => x.Coordinate).Returns("Status");
        change.As<IEnumModifiedChange>()
            .SetupGet(x => x.__typename).Returns("EnumModifiedChange");
        change.As<IEnumModifiedChange>()
            .SetupGet(x => x.Changes).Returns(subChanges);
        change.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Dangerous);
        change.SetupGet(x => x.__typename).Returns("EnumModifiedChange");

        return change;
    }

    private static Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>
        CreateTypeSystemMemberAddedChange()
    {
        var change = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>(
            MockBehavior.Strict);
        change
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes>();
        change.As<ITypeSystemMemberAddedChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Safe);
        change.As<ITypeSystemMemberAddedChange>()
            .SetupGet(x => x.Coordinate).Returns("NewType");
        change.As<ITypeSystemMemberAddedChange>()
            .SetupGet(x => x.__typename).Returns("TypeSystemMemberAddedChange");
        change.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Safe);
        change.SetupGet(x => x.__typename).Returns("TypeSystemMemberAddedChange");

        return change;
    }

    private static Mock<IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>
        CreateTypeSystemMemberRemovedChange()
    {
        var change = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_Changes>(
            MockBehavior.Strict);
        change
            .As<IOnFusionConfigurationPublishingTaskChanged_OnFusionConfigurationPublishingTaskChanged_Errors_Changes>();
        change.As<ITypeSystemMemberRemovedChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        change.As<ITypeSystemMemberRemovedChange>()
            .SetupGet(x => x.Coordinate).Returns("OldType");
        change.As<ITypeSystemMemberRemovedChange>()
            .SetupGet(x => x.__typename).Returns("TypeSystemMemberRemovedChange");
        change.As<ISchemaChange>()
            .SetupGet(x => x.Severity).Returns(SchemaChangeSeverity.Breaking);
        change.SetupGet(x => x.__typename).Returns("TypeSystemMemberRemovedChange");

        return change;
    }

    #endregion

    #region Deployment Setup Methods

    public static void SetupSchemaDeployment<T>(Mock<T> mock) where T : class
    {
        // 1. InvalidGraphQLSchemaError
        var invalidGraphql = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4>(
            MockBehavior.Strict);
        SetupInvalidGraphQLSchemaError(invalidGraphql);

        // 2. SchemaChangeViolationError
        var schemaChange = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4>(
            MockBehavior.Strict);
        SetupSchemaChangeViolationError(schemaChange);

        // 3. PersistedQueryValidationError
        var pqError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4>(
            MockBehavior.Strict);
        SetupPersistedQueryValidationError(pqError);

        // 4. OpenApiCollectionValidationError
        var openApiError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4>(
            MockBehavior.Strict);
        SetupOpenApiCollectionValidationError(openApiError);

        // 5. McpFeatureCollectionValidationError
        var mcpError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4>(
            MockBehavior.Strict);
        SetupMcpFeatureCollectionValidationError(mcpError);

        // 6. SchemaVersionSyntaxError
        var syntaxError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4>(
            MockBehavior.Strict);
        SetupSchemaVersionSyntaxError(syntaxError);

        // 7. OperationsAreNotAllowedError
        var opsError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4>(
            MockBehavior.Strict);
        SetupOperationsAreNotAllowedError(opsError);

        mock.As<ISchemaDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_4[]
            {
                invalidGraphql.Object,
                schemaChange.Object,
                pqError.Object,
                openApiError.Object,
                mcpError.Object,
                syntaxError.Object,
                opsError.Object
            });
    }

    public static void SetupFusionDeployment<T>(Mock<T> mock) where T : class
    {
        // 1. InvalidGraphQLSchemaError
        var invalidGraphql = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_1>(
            MockBehavior.Strict);
        SetupInvalidGraphQLSchemaError(invalidGraphql);

        // 2. SchemaChangeViolationError
        var schemaChange = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_1>(
            MockBehavior.Strict);
        SetupSchemaChangeViolationError(schemaChange);

        // 3. PersistedQueryValidationError
        var pqError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_1>(
            MockBehavior.Strict);
        SetupPersistedQueryValidationError(pqError);

        // 4. OpenApiCollectionValidationError
        var openApiError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_1>(
            MockBehavior.Strict);
        SetupOpenApiCollectionValidationError(openApiError);

        // 5. McpFeatureCollectionValidationError
        var mcpError = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_1>(
            MockBehavior.Strict);
        SetupMcpFeatureCollectionValidationError(mcpError);

        mock.As<IFusionConfigurationDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_1[]
            {
                invalidGraphql.Object,
                schemaChange.Object,
                pqError.Object,
                openApiError.Object,
                mcpError.Object
            });
    }

    public static void SetupClientDeployment<T>(Mock<T> mock) where T : class
    {
        var deploymentErrorMock = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors>(
            MockBehavior.Strict);
        SetupClientPersistedQueryValidationError(deploymentErrorMock);

        mock.As<IClientDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { deploymentErrorMock.Object });
    }

    public static void SetupOpenApiCollectionDeployment<T>(Mock<T> mock) where T : class
    {
        var errorMock = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_3>(
            MockBehavior.Strict);
        SetupOpenApiCollectionValidationError(errorMock);

        mock.As<IOpenApiCollectionDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { errorMock.Object });
    }

    public static void SetupMcpFeatureCollectionDeployment<T>(
        Mock<T> mock,
        string docErrorMessage = "The field `person` does not exist on the type `Query`.") where T : class
    {
        var errorMock = new Mock<
            IOnClientVersionPublishUpdated_OnClientVersionPublishingUpdate_Deployment_Errors_2>(
            MockBehavior.Strict);
        SetupMcpFeatureCollectionValidationError(errorMock, docErrorMessage);

        mock.As<IMcpFeatureCollectionDeployment>()
            .SetupGet(x => x.Errors)
            .Returns(new[] { errorMock.Object });
    }

    #endregion
}
