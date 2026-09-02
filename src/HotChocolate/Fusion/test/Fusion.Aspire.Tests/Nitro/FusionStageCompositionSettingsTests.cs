using System.Text.Json;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class FusionStageCompositionSettingsTests : IAsyncLifetime
{
    private FakeNitroServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FakeNitroServer.StartAsync();

    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task GetStageCompositionSettingsAsync_Should_SendTheOperation_When_ItIsCalled()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """{"data":{"node":{"stage":{"compositionSettings":null}}}}""");
        using var api = CreateApi();

        // act
        await api.GetStageCompositionSettingsAsync(
            CreateTarget(),
            "production",
            TestContext.Current.CancellationToken);

        // assert
        var request = Assert.Single(_server.Requests);

        Assert.Equal("nitro-api-key", request.Headers[NitroRequestHeaders.ApiKey]);
#if !NITRO_PERSISTED_OPERATIONS
        request.Body.MatchInlineSnapshot(
            """
            {"query":"query GetNitroCompositionSettings($apiId: ID!, $stageName: String!) {\n  node(id: $apiId) {\n    ... on Api {\n      stage(name: $stageName) {\n        compositionSettings {\n          cacheControlMergeBehavior\n          enableGlobalObjectIdentification\n          excludeByTag\n          nodeResolution\n          removeUnreferencedDefinitions\n          tagMergeBehavior\n        }\n      }\n    }\n  }\n}\n","operationName":"GetNitroCompositionSettings","variables":{"apiId":"api-1","stageName":"production"}}
            """);
#else
        request.Body.MatchInlineSnapshot(
            """
            {"id":"576b1d39b8ed179da29e50247574aaae26d979591a4bbe53fcaf850fe2b02351","operationName":"GetNitroCompositionSettings","variables":{"apiId":"api-1","stageName":"production"}}
            """);
#endif
    }

    [Fact]
    public async Task GetStageCompositionSettingsAsync_Should_ReadSettings_When_StageDeclaresThem()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """
            {"data":{"node":{"stage":{"compositionSettings":{
              "cacheControlMergeBehavior":"INCLUDE_PRIVATE",
              "enableGlobalObjectIdentification":true,
              "excludeByTag":["internal"],
              "nodeResolution":"SOURCE_SCHEMA",
              "removeUnreferencedDefinitions":true,
              "tagMergeBehavior":"IGNORE"}}}}}
            """);
        using var api = CreateApi();

        // act
        var settings = await api.GetStageCompositionSettingsAsync(
            CreateTarget(),
            "production",
            TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(settings);
        JsonSerializer.Serialize(
                JsonSerializer.SerializeToDocument(
                        settings,
                        SettingsJsonSerializerContext.Default.CompositionSettings)
                    .RootElement,
                new JsonSerializerOptions { WriteIndented = true })
            .MatchInlineSnapshot(
                """
                {
                  "preprocessor": {
                    "excludeByTag": [
                      "internal"
                    ]
                  },
                  "merger": {
                    "addFusionDefinitions": null,
                    "cacheControlMergeBehavior": "IncludePrivate",
                    "enableGlobalObjectIdentification": true,
                    "enumValuesMergeBehavior": null,
                    "nodeResolution": "SourceSchema",
                    "removeUnreferencedDefinitions": true,
                    "tagMergeBehavior": "Ignore"
                  },
                  "satisfiability": {
                    "includeSatisfiabilityPaths": null
                  },
                  "apolloFederationCompatibility": {
                    "allowNonResolvableInterfaceObjects": null,
                    "shareableFieldRuntimeTypeRouting": null
                  }
                }
                """);
    }

    [Fact]
    public async Task
        GetStageCompositionSettingsAsync_Should_ReturnNull_When_StageDeclaresNoSettings()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """{"data":{"node":{"stage":{"compositionSettings":null}}}}""");
        using var api = CreateApi();

        // act
        var settings = await api.GetStageCompositionSettingsAsync(
            CreateTarget(),
            "production",
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(settings);
    }

    [Fact]
    public async Task
        TryGetStageCompositionSettingsAsync_Should_WarnAndYieldNull_When_FieldIsUnknown()
    {
        // arrange
        // a Nitro server that predates the stage composition settings rejects the field
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """
            {"errors":[{"message":"The field `compositionSettings` does not exist.",
              "extensions":{"code":"HC0020"}}]}
            """);
        using var api = CreateApi();
        var workflow = new FusionDeploymentWorkflow(api);
        var logger = new RecordingLogger<FusionStageCompositionSettingsTests>();

        // act
        var settings = await workflow.TryGetStageCompositionSettingsAsync(
            CreateTarget(),
            "production",
            logger,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Null(settings);
        Assert.Equal(
            "The composition settings of stage production could not be downloaded, so the "
            + "composition only uses the settings of the distributed application. Update the "
            + "Nitro server to the latest version. Nitro returned GraphQL errors for the "
            + "composition settings operation: The field `compositionSettings` does not exist.",
            Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task
        TryGetStageCompositionSettingsAsync_Should_Throw_When_ServerRejectsTheRequest()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """{"errors":[{"message":"The current user is not authorized."}]}""");
        using var api = CreateApi();
        var workflow = new FusionDeploymentWorkflow(api);

        // act
        var exception = await Assert.ThrowsAsync<NitroOperationException>(
            () => workflow.TryGetStageCompositionSettingsAsync(
                CreateTarget(),
                "production",
                new RecordingLogger<FusionStageCompositionSettingsTests>(),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "Nitro returned GraphQL errors for the composition settings operation: "
            + "The current user is not authorized.",
            exception.Message);
    }

    private static NitroFusionApi CreateApi()
        => new(new HttpClient(), disposeHttpClient: true);

    private FusionTarget CreateTarget()
        => new(_server.BaseAddress, "api-1", "nitro-api-key");
}
