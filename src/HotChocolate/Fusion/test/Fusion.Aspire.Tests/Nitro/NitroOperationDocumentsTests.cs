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
#endif
}
