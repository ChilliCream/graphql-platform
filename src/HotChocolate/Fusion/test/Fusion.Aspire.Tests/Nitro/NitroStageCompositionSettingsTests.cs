using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroStageCompositionSettingsTests : IAsyncLifetime
{
    private FakeNitroServer _server = null!;

    public async ValueTask InitializeAsync() => _server = await FakeNitroServer.StartAsync();

    public async ValueTask DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task GetStageCompositionSettingsAsync_Should_SendTheOperation_When_ItIsCalled()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """{"data":{"apiById":{"stage":{"compositionSettings":null}}}}""");
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
            {"query":"query GetFusionStageCompositionSettings($apiId: ID!, $stageName: String!) {\n  apiById(id: $apiId) {\n    stage(name: $stageName) {\n      compositionSettings {\n        cacheControlMergeBehavior\n        enableGlobalObjectIdentification\n        excludeByTag\n        nodeResolution\n        removeUnreferencedDefinitions\n        tagMergeBehavior\n      }\n    }\n  }\n}\n","operationName":"GetFusionStageCompositionSettings","variables":{"apiId":"api-1","stageName":"production"}}
            """);
#else
        request.Body.MatchInlineSnapshot(
            """
            {"id":"5cf6fc33bd9b6b535673f8adf0163b1ec7daaa7c8ed25240f77a9e814a7b6b7a","operationName":"GetFusionStageCompositionSettings","variables":{"apiId":"api-1","stageName":"production"}}
            """);
#endif
    }

    [Fact]
    public async Task GetStageCompositionSettingsAsync_Should_ReadSettings_When_StageDeclaresThem()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """
            {"data":{"apiById":{"stage":{"compositionSettings":{
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
                        settings.ToCompositionSettings(),
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
    public async Task GetStageCompositionSettingsAsync_Should_ReturnNull_When_StageIsUnknown()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """{"data":{"apiById":{"stage":null}}}""");
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
    public async Task GetStageCompositionSettingsAsync_Should_Throw_When_ServerRejectsTheRequest()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """{"errors":[{"message":"The current user is not authorized."}]}""");
        using var api = CreateApi();

        // act
        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => api.GetStageCompositionSettingsAsync(
                CreateTarget(),
                "production",
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "Nitro returned GraphQL errors: The current user is not authorized.",
            exception.Message);
    }

    [Fact]
    public async Task GetStageCompositionSettingsAsync_Should_Throw_When_FieldIsUnknown()
    {
        // arrange
        // a Nitro server that predates the stage composition settings rejects the field
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """
            {"errors":[{"message":"The field `compositionSettings` does not exist.",
              "extensions":{"code":"HC0020"}}]}
            """);
        using var api = CreateApi();

        // act
        var exception = await Assert.ThrowsAsync<FusionOperationUnsupportedException>(
            () => api.GetStageCompositionSettingsAsync(
                CreateTarget(),
                "production",
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "Nitro returned GraphQL errors: The field `compositionSettings` does not exist.",
            exception.Message);
    }

    [Fact]
    public async Task
        TryGetStageCompositionSettingsAsync_Should_WarnAndYieldNull_When_FieldIsUnknown()
    {
        // arrange
        _server.GraphQLHandler = _ => FakeNitroResponse.Json(
            """
            {"errors":[{"message":"The field `compositionSettings` does not exist.",
              "extensions":{"code":"HC0020"}}]}
            """);
        using var api = CreateApi();
        var workflow = new FusionDeploymentWorkflow(api);
        var logger = new RecordingLogger<NitroStageCompositionSettingsTests>();

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
            + "Nitro server to the latest version. Nitro returned GraphQL errors: The field "
            + "`compositionSettings` does not exist.",
            Assert.Single(logger.Entries).Message);
    }

    [Fact]
    public async Task TryGetStageCompositionSettingsAsync_Should_Throw_When_ServerFails()
    {
        // arrange
        _server.GraphQLHandler = _ =>
            FakeNitroResponse.Status(StatusCodes.Status500InternalServerError);
        using var api = CreateApi();
        var workflow = new FusionDeploymentWorkflow(api);

        // act
        var exception = await Assert.ThrowsAsync<FusionDeploymentException>(
            () => workflow.TryGetStageCompositionSettingsAsync(
                CreateTarget(),
                "production",
                new RecordingLogger<NitroStageCompositionSettingsTests>(),
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal(
            "Nitro returned HTTP 500 (InternalServerError).",
            exception.Message);
    }

    private static NitroFusionApi CreateApi()
        => new(new HttpClient(), disposeHttpClient: true);

    private FusionTarget CreateTarget()
        => new(_server.BaseAddress, "api-1", "nitro-api-key");
}
