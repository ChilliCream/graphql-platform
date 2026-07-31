using System.Security.Cryptography;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroOperationDocumentsTests
{
    /// <summary>
    /// The persisted operation id of every operation. The ids are the sidecars that
    /// <c>.github/scripts/nitro-aspire-operations.sh</c> generates and that Nitro registers, so
    /// they are pinned here rather than recomputed.
    /// </summary>
    public static TheoryData<string, string> OperationIds =>
        new()
        {
            {
                NitroOperationDocuments.ResolveApiNameOperationName,
                "6480d908b5fe1cad49198376e03c5547c83234d90c154c9566324717cda41f45"
            },
            {
                NitroOperationDocuments.ValidateSchemaOperationName,
                "89e8800b8720401041061528aef1101683b241d7d24c7f0912928a1a901def02"
            },
            {
                NitroOperationDocuments.WatchSchemaValidationOperationName,
                "ad7ee984bdd62c1357d5ee29261adff3f60bb119f5f162ae1ee8a1f9c62632d9"
            },
            {
                NitroOperationDocuments.UploadSourceSchemaOperationName,
                "f091e081d5fea136c590aa34b6f37d391d69844c23f3a61f5e728ba97fed951f"
            },
            {
                NitroOperationDocuments.BeginDeploymentOperationName,
                "d858bf7df2ee5df613f2e699446657dd3bd489b9bd1d246eff7bf021c31307c7"
            },
            {
                NitroOperationDocuments.ClaimDeploymentOperationName,
                "5b818ded677899729e77ceeba1fc6ae179a08cf7982d158fcc2731f798614046"
            },
            {
                NitroOperationDocuments.ValidateDeploymentOperationName,
                "de83bb4fc363ce531c18138aa35938387f46a81b1258f8882dd7c67b6c499a5e"
            },
            {
                NitroOperationDocuments.CommitDeploymentOperationName,
                "f66f07ad033aff0963053bd465ff33463b48b2c4888a30b7db7dfbb8301f48ec"
            },
            {
                NitroOperationDocuments.ReleaseDeploymentOperationName,
                "808c0252d2713db0f747a5c6d8d9e56806db032489daac04d9b075c1baef06a2"
            },
            {
                NitroOperationDocuments.WatchDeploymentOperationName,
                "25fff4ef7a751b2d9b1008f353dc23c89b8aed8978334407d3aa9a83806839b1"
            }
        };

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

    [Theory]
    [InlineData("ResolveNitroApiName", "query ResolveNitroApiName(")]
    [InlineData("ValidateNitroSchema", "mutation ValidateNitroSchema(")]
    [InlineData("WatchNitroSchemaValidation", "subscription WatchNitroSchemaValidation(")]
    [InlineData("UploadFusionSourceSchema", "mutation UploadFusionSourceSchema(")]
    [InlineData("BeginFusionDeployment", "mutation BeginFusionDeployment(")]
    [InlineData("ClaimFusionDeployment", "mutation ClaimFusionDeployment(")]
    [InlineData("ValidateFusionDeployment", "mutation ValidateFusionDeployment(")]
    [InlineData("CommitFusionDeployment", "mutation CommitFusionDeployment(")]
    [InlineData("ReleaseFusionDeployment", "mutation ReleaseFusionDeployment(")]
    [InlineData("WatchFusionDeployment", "subscription WatchFusionDeployment(")]
    public void OperationName_Should_MatchTheOperationInTheDocument(
        string operationName,
        string expectedDeclaration)
    {
        // act
        var document = GetDocument(operationName);

        // assert
        Assert.Contains(expectedDeclaration, document, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(OperationIds))]
    public void GetDocument_Should_HashToTheOperationId(
        string operationName,
        string expectedOperationId)
    {
        // act
        // the document that is sent on the wire has to be exactly the document that the sidecar
        // was computed over, otherwise a release build sends an id that Nitro never persisted.
        var document = Utf8GraphQLParser.Parse(GetDocument(operationName)).ToString(false);
        var operationId = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(document)));

        // assert
        Assert.Equal(expectedOperationId, operationId);
    }

    private static string GetDocument(string operationName)
        => operationName switch
        {
            NitroOperationDocuments.ResolveApiNameOperationName
                => NitroOperationDocuments.GetResolveApiNameDocument(),
            NitroOperationDocuments.ValidateSchemaOperationName
                => NitroOperationDocuments.GetValidateSchemaDocument(),
            NitroOperationDocuments.WatchSchemaValidationOperationName
                => NitroOperationDocuments.GetWatchSchemaValidationDocument(),
            NitroOperationDocuments.UploadSourceSchemaOperationName
                => NitroOperationDocuments.GetUploadSourceSchemaDocument(),
            NitroOperationDocuments.BeginDeploymentOperationName
                => NitroOperationDocuments.GetBeginDeploymentDocument(),
            NitroOperationDocuments.ClaimDeploymentOperationName
                => NitroOperationDocuments.GetClaimDeploymentDocument(),
            NitroOperationDocuments.ValidateDeploymentOperationName
                => NitroOperationDocuments.GetValidateDeploymentDocument(),
            NitroOperationDocuments.CommitDeploymentOperationName
                => NitroOperationDocuments.GetCommitDeploymentDocument(),
            NitroOperationDocuments.ReleaseDeploymentOperationName
                => NitroOperationDocuments.GetReleaseDeploymentDocument(),
            NitroOperationDocuments.WatchDeploymentOperationName
                => NitroOperationDocuments.GetWatchDeploymentDocument(),
            _ => throw new ArgumentOutOfRangeException(nameof(operationName))
        };
#endif

#if NITRO_PERSISTED_OPERATIONS
    [Theory]
    [MemberData(nameof(OperationIds))]
    public void GetOperationId_Should_ReturnTheEmbeddedHash(
        string operationName,
        string expectedOperationId)
    {
        // act
        var operationId = GetOperationId(operationName);

        // assert
        Assert.Equal(expectedOperationId, operationId);
    }

    private static string GetOperationId(string operationName)
        => operationName switch
        {
            NitroOperationDocuments.ResolveApiNameOperationName
                => NitroOperationDocuments.GetResolveApiNameOperationId(),
            NitroOperationDocuments.ValidateSchemaOperationName
                => NitroOperationDocuments.GetValidateSchemaOperationId(),
            NitroOperationDocuments.WatchSchemaValidationOperationName
                => NitroOperationDocuments.GetWatchSchemaValidationOperationId(),
            NitroOperationDocuments.UploadSourceSchemaOperationName
                => NitroOperationDocuments.GetUploadSourceSchemaOperationId(),
            NitroOperationDocuments.BeginDeploymentOperationName
                => NitroOperationDocuments.GetBeginDeploymentOperationId(),
            NitroOperationDocuments.ClaimDeploymentOperationName
                => NitroOperationDocuments.GetClaimDeploymentOperationId(),
            NitroOperationDocuments.ValidateDeploymentOperationName
                => NitroOperationDocuments.GetValidateDeploymentOperationId(),
            NitroOperationDocuments.CommitDeploymentOperationName
                => NitroOperationDocuments.GetCommitDeploymentOperationId(),
            NitroOperationDocuments.ReleaseDeploymentOperationName
                => NitroOperationDocuments.GetReleaseDeploymentOperationId(),
            NitroOperationDocuments.WatchDeploymentOperationName
                => NitroOperationDocuments.GetWatchDeploymentOperationId(),
            _ => throw new ArgumentOutOfRangeException(nameof(operationName))
        };
#endif
}
