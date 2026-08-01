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
                "22190a0146ff00a9719826ea025019ea279b61a1be0d88b0b21015acd1f88dc4"
            },
            {
                NitroOperationDocuments.GetStageVersionOperationName,
                "32cc7d1ee75aaa16627d21ee9289b4758f805d8b1eedfa796d832bae05b840f1"
            },
            {
                NitroOperationDocuments.WatchStageOperationName,
                "6626c4ef1d416a1db6a56bed1f19ab68f8797bb3e9bf79b31d2434b41a0d1d6e"
            },
            {
                NitroOperationDocuments.UploadSourceSchemaOperationName,
                "1114c593d527acede0596dc456f59242f899101ed4c089314adaa60cdddcff79"
            },
            {
                NitroOperationDocuments.BeginDeploymentOperationName,
                "ceab775204b86a28d147d5799f39b5b05cd2e7abb93d337df315432f5b8a32ac"
            },
            {
                NitroOperationDocuments.ClaimDeploymentOperationName,
                "1efc9d275a44832a90a1a5227e280de66582e21e4e83bc89ae08fe8f0b3c69e1"
            },
            {
                NitroOperationDocuments.ValidateDeploymentOperationName,
                "e5d213965c3e14e470ba31e9c238147ebd35fcec42b636960a7c2dcc535e1325"
            },
            {
                NitroOperationDocuments.CommitDeploymentOperationName,
                "74a2f5e237fb7213158efed61f9b3e48d420bb3fe45b2f0bce82ecff64319edd"
            },
            {
                NitroOperationDocuments.ReleaseDeploymentOperationName,
                "0ce52927ce212dfd36404a8a74db96c06be1e791845c526d8c0b0a8ded1a4a61"
            },
            {
                NitroOperationDocuments.WatchDeploymentOperationName,
                "25fff4ef7a751b2d9b1008f353dc23c89b8aed8978334407d3aa9a83806839b1"
            },
            {
                NitroOperationDocuments.GetCompositionSettingsOperationName,
                "576b1d39b8ed179da29e50247574aaae26d979591a4bbe53fcaf850fe2b02351"
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
    [InlineData("GetNitroStageVersion", "query GetNitroStageVersion(")]
    [InlineData("WatchNitroStage", "subscription WatchNitroStage(")]
    [InlineData("UploadFusionSourceSchema", "mutation UploadFusionSourceSchema(")]
    [InlineData("BeginFusionDeployment", "mutation BeginFusionDeployment(")]
    [InlineData("ClaimFusionDeployment", "mutation ClaimFusionDeployment(")]
    [InlineData("ValidateFusionDeployment", "mutation ValidateFusionDeployment(")]
    [InlineData("CommitFusionDeployment", "mutation CommitFusionDeployment(")]
    [InlineData("ReleaseFusionDeployment", "mutation ReleaseFusionDeployment(")]
    [InlineData("WatchFusionDeployment", "subscription WatchFusionDeployment(")]
    [InlineData("GetNitroCompositionSettings", "query GetNitroCompositionSettings(")]
    public void OperationName_Should_MatchTheOperationInTheDocument(
        string operationName,
        string expectedDeclaration)
    {
        // act
        var document = GetDocument(operationName);

        // assert
        Assert.Contains(expectedDeclaration, document, StringComparison.Ordinal);
    }

    [Fact]
    public void GetWatchSchemaValidationDocument_Should_RequestNestedSchemaChangeDetails()
    {
        // act
        var document = NitroOperationDocuments.GetWatchSchemaValidationDocument();

        // assert
        // the fragments must be defined and actually spread into the selections
        Assert.Contains(
            "fragment SchemaChangeDetail on SchemaChange",
            document,
            StringComparison.Ordinal);
        Assert.Contains(
            "fragment SchemaChangeLeaf on SchemaChange",
            document,
            StringComparison.Ordinal);
        Assert.Contains("...SchemaChangeDetail", document, StringComparison.Ordinal);
        Assert.Contains("...SchemaChangeLeaf", document, StringComparison.Ordinal);
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
            NitroOperationDocuments.GetStageVersionOperationName
                => NitroOperationDocuments.GetStageVersionDocument(),
            NitroOperationDocuments.WatchStageOperationName
                => NitroOperationDocuments.GetWatchStageDocument(),
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
            NitroOperationDocuments.GetCompositionSettingsOperationName
                => NitroOperationDocuments.GetCompositionSettingsDocument(),
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
            NitroOperationDocuments.GetStageVersionOperationName
                => NitroOperationDocuments.GetStageVersionOperationId(),
            NitroOperationDocuments.WatchStageOperationName
                => NitroOperationDocuments.GetWatchStageOperationId(),
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
            NitroOperationDocuments.GetCompositionSettingsOperationName
                => NitroOperationDocuments.GetCompositionSettingsOperationId(),
            _ => throw new ArgumentOutOfRangeException(nameof(operationName))
        };
#endif
}
