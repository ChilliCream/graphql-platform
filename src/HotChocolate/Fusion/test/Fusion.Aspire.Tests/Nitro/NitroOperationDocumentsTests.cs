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

    [Theory]
    [InlineData(
        "ValidateNitroSchema",
        "89e8800b8720401041061528aef1101683b241d7d24c7f0912928a1a901def02")]
    [InlineData(
        "PollNitroSchemaValidation",
        "6ebe506f2dda96523ef2a0302b7b4aa0f748c2ace6dd5b89449c081c7dbb135e")]
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
#endif
}
