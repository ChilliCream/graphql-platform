using System.Text;
using System.Text.Json;
using ChilliCream.Nitro.CommandLine.Commands.Fusion;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Fusion;

public sealed class FusionMigrationHelpersTests
{
    #region MigrateSubgraphConfig

    [Fact]
    public void MigrateSubgraphConfig_Should_WriteEmptyName_When_InputIsEmptyObject()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("{}");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"version":"1.0.0","name":""}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_RenameSubgraphToName_When_SubgraphPropertyPresent()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"subgraph":"products"}""");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"version":"1.0.0","name":"products"}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_RenameHttpBaseAddressToUrlUnderTransports_When_HttpPresent()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes(
            """{"subgraph":"products","http":{"baseAddress":"http://localhost/graphql"}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"version":"1.0.0","name":"products","transports":{"http":{"url":"http://localhost/graphql"}}}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_PreserveOtherHttpProperties_When_HttpHasMoreThanBaseAddress()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes(
            """{"subgraph":"products","http":{"baseAddress":"http://x/gql","clientName":"Fusion","timeout":"00:00:30"}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"version":"1.0.0","name":"products","transports":{"http":{"url":"http://x/gql","clientName":"Fusion","timeout":"00:00:30"}}}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_PreserveHttpProperties_When_BaseAddressIsMissing()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes(
            """{"subgraph":"products","http":{"clientName":"Fusion"}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"version":"1.0.0","name":"products","transports":{"http":{"clientName":"Fusion"}}}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_DropWebsocketProperty_When_WebsocketPresent()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes(
            """{"subgraph":"products","websocket":{"baseAddress":"ws://localhost/ws"}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"version":"1.0.0","name":"products"}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_PreserveUnknownTopLevelProperties_When_AdditionalFieldsPresent()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes(
            """{"subgraph":"products","extensions":{"custom":true},"retryCount":3}""");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"version":"1.0.0","name":"products","extensions":{"custom":true},"retryCount":3}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_ProduceFullMigration_When_AllKnownPropertiesPresent()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes(
            """
            {
              "subgraph": "products",
              "http": { "baseAddress": "http://x/gql", "clientName": "Fusion" },
              "websocket": { "baseAddress": "ws://x/ws" },
              "extensions": { "custom": true }
            }
            """);

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"version":"1.0.0","name":"products","transports":{"http":{"url":"http://x/gql","clientName":"Fusion"}},"extensions":{"custom":true}}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_PreserveExistingVersion_When_VersionAlreadyPresent()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"subgraph":"products","version":"9.9.9"}""");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"name":"products","version":"9.9.9"}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_InjectDefaultVersion_When_VersionMissing()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"subgraph":"products"}""");

        // act
        using var result = FusionMigrationHelpers.MigrateSubgraphConfig(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"version":"1.0.0","name":"products"}""");
    }

    [Fact]
    public void MigrateSubgraphConfig_Should_Throw_When_InputIsInvalidJson()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("not json");

        // act & assert
        Assert.ThrowsAny<JsonException>(() => FusionMigrationHelpers.MigrateSubgraphConfig(input));
    }

    #endregion

    #region MigrateGatewaySettings

    [Fact]
    public void MigrateGatewaySettings_Should_EmitDefaultScaffolding_When_InputIsEmptyObject()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("{}");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"preprocessor":{},"merger":{},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_AddEnableGlobalObjectIdentificationTrue_When_NodeFieldEnabled()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"nodeField":{"enabled":true}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"preprocessor":{},"merger":{"enableGlobalObjectIdentification":true},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_AddEnableGlobalObjectIdentificationFalse_When_NodeFieldDisabled()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"nodeField":{"enabled":false}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"preprocessor":{},"merger":{"enableGlobalObjectIdentification":false},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_OmitEnableGlobalObjectIdentification_When_NodeFieldHasNoEnabled()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"nodeField":{}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"preprocessor":{},"merger":{},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_AddTagMergeBehaviorInclude_When_TagDirectiveMakePublicTrue()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"tagDirective":{"makePublic":true}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"preprocessor":{},"merger":{"tagMergeBehavior":"Include"},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_OmitTagMergeBehavior_When_TagDirectiveMakePublicFalse()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"tagDirective":{"makePublic":false}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"preprocessor":{},"merger":{},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_AddExcludeByTag_When_TagDirectiveExcludeNonEmpty()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"tagDirective":{"exclude":["internal","beta"]}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"preprocessor":{"excludeByTag":["internal","beta"]},"merger":{},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_OmitExcludeByTag_When_TagDirectiveExcludeEmpty()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"tagDirective":{"exclude":[]}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot("""{"preprocessor":{},"merger":{},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_OmitExcludeByTag_When_TagDirectiveExcludeMissing()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("""{"tagDirective":{"makePublic":true}}""");

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"preprocessor":{},"merger":{"tagMergeBehavior":"Include"},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_CombineAllMigrations_When_AllRelevantPropertiesPresent()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes(
            """
            {
              "tagDirective": { "makePublic": true, "exclude": ["internal"] },
              "nodeField": { "enabled": true },
              "transport": { "defaultClientName": "Fusion" },
              "reEncodeIds": { "enabled": true }
            }
            """);

        // act
        using var result = FusionMigrationHelpers.MigrateGatewaySettings(input);

        // assert
        Serialize(result).MatchInlineSnapshot(
            """{"preprocessor":{"excludeByTag":["internal"]},"merger":{"enableGlobalObjectIdentification":true,"tagMergeBehavior":"Include"},"satisfiability":{}}""");
    }

    [Fact]
    public void MigrateGatewaySettings_Should_Throw_When_InputIsInvalidJson()
    {
        // arrange
        var input = Encoding.UTF8.GetBytes("not json");

        // act & assert
        Assert.ThrowsAny<JsonException>(() => FusionMigrationHelpers.MigrateGatewaySettings(input));
    }

    #endregion

    private static string Serialize(JsonDocument document)
        => JsonSerializer.Serialize(document.RootElement);
}
