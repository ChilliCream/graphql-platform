namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroOperationDocumentsTests
{
#if !NITRO_PERSISTED_OPERATIONS
    [Fact]
    public void GetResolveApiNameDocument_Should_ReturnTheEmbeddedDocument()
    {
        // act
        var document = NitroOperationDocuments.GetResolveApiNameDocument();

        // assert
        document.MatchInlineSnapshot(
            """
            query ResolveNitroApiName($id: ID!) {
              node(id: $id) {
                ... on Api {
                  name
                }
              }
            }

            """);
    }

    [Fact]
    public void ResolveApiNameOperationName_Should_MatchTheOperationInTheDocument()
    {
        // act
        var document = NitroOperationDocuments.GetResolveApiNameDocument();

        // assert
        Assert.Contains(
            $"query {NitroOperationDocuments.ResolveApiNameOperationName}(",
            document,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GetCompositionSettingsOperationName_Should_MatchTheOperationInTheDocument()
    {
        // act
        var document = NitroOperationDocuments.GetCompositionSettingsDocument();

        // assert
        Assert.Contains(
            $"query {NitroOperationDocuments.GetCompositionSettingsOperationName}(",
            document,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("ValidateNitroSchema", "mutation ValidateNitroSchema(")]
    [InlineData("PollNitroSchemaValidation", "mutation PollNitroSchemaValidation(")]
    public void ValidationOperationName_Should_MatchTheOperationInTheDocument(
        string operationName,
        string expectedDeclaration)
    {
        // act
        var document = operationName == NitroOperationDocuments.ValidateSchemaOperationName
            ? NitroOperationDocuments.GetValidateSchemaDocument()
            : NitroOperationDocuments.GetPollSchemaValidationDocument();

        // assert
        Assert.Contains(expectedDeclaration, document, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("GetNitroStageVersion", "query GetNitroStageVersion(")]
    [InlineData("WatchNitroStage", "subscription WatchNitroStage(")]
    public void StageUpdateOperationName_Should_MatchTheOperationInTheDocument(
        string operationName,
        string expectedDeclaration)
    {
        // act
        var document = operationName == NitroOperationDocuments.GetStageVersionOperationName
            ? NitroOperationDocuments.GetStageVersionDocument()
            : NitroOperationDocuments.GetWatchStageDocument();

        // assert
        Assert.Contains(expectedDeclaration, document, StringComparison.Ordinal);
    }

#endif

#if NITRO_PERSISTED_OPERATIONS
    [Fact]
    public void GetResolveApiNameOperationId_Should_ReturnTheEmbeddedHash()
    {
        // act
        var operationId = NitroOperationDocuments.GetResolveApiNameOperationId();

        // assert
        Assert.Equal(
            "6480d908b5fe1cad49198376e03c5547c83234d90c154c9566324717cda41f45",
            operationId);
    }

    [Fact]
    public void GetCompositionSettingsOperationId_Should_ReturnTheEmbeddedHash()
    {
        // act
        var operationId = NitroOperationDocuments.GetCompositionSettingsOperationId();

        // assert
        Assert.Equal(
            "576b1d39b8ed179da29e50247574aaae26d979591a4bbe53fcaf850fe2b02351",
            operationId);
    }

    [Theory]
    [InlineData(
        "ValidateNitroSchema",
        "89e8800b8720401041061528aef1101683b241d7d24c7f0912928a1a901def02")]
    [InlineData(
        "PollNitroSchemaValidation",
        "0fcb0a11b85be6ede7bb6141f0cdcc34a3fc7eaa611747d013d02b9374e48e22")]
    public void GetValidationOperationId_Should_ReturnTheEmbeddedHash(
        string operationName,
        string expectedOperationId)
    {
        // act
        var operationId = operationName == NitroOperationDocuments.ValidateSchemaOperationName
            ? NitroOperationDocuments.GetValidateSchemaOperationId()
            : NitroOperationDocuments.GetPollSchemaValidationOperationId();

        // assert
        Assert.Equal(expectedOperationId, operationId);
    }

    [Theory]
    [InlineData(
        "GetNitroStageVersion",
        "32cc7d1ee75aaa16627d21ee9289b4758f805d8b1eedfa796d832bae05b840f1")]
    [InlineData(
        "WatchNitroStage",
        "6626c4ef1d416a1db6a56bed1f19ab68f8797bb3e9bf79b31d2434b41a0d1d6e")]
    public void GetStageUpdateOperationId_Should_ReturnTheEmbeddedHash(
        string operationName,
        string expectedOperationId)
    {
        // act
        var operationId = operationName == NitroOperationDocuments.GetStageVersionOperationName
            ? NitroOperationDocuments.GetStageVersionOperationId()
            : NitroOperationDocuments.GetWatchStageOperationId();

        // assert
        Assert.Equal(expectedOperationId, operationId);
    }
#endif
}
