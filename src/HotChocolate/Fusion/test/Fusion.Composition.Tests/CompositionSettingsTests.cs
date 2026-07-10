using System.Text.Json;

namespace HotChocolate.Fusion;

public sealed class CompositionSettingsTests
{
    [Fact]
    public void ApolloFederationCompatibility_Should_RoundTrip_When_Serialized()
    {
        // arrange
        var settings = new CompositionSettings
        {
            ApolloFederationCompatibility =
            {
                AllowNonResolvableInterfaceObjects = true
            }
        };

        // act
        using var document = JsonSerializer.SerializeToDocument(
            settings,
            SettingsJsonSerializerContext.Default.CompositionSettings);
        var roundTripped = document.Deserialize(
            SettingsJsonSerializerContext.Default.CompositionSettings);

        // assert
        Assert.True(document.RootElement
            .GetProperty("apolloFederationCompatibility")
            .GetProperty("allowNonResolvableInterfaceObjects")
            .GetBoolean());
        Assert.True(roundTripped!.ApolloFederationCompatibility.AllowNonResolvableInterfaceObjects);
        Assert.True(roundTripped.ApolloFederationCompatibility
            .ToOptions()
            .AllowNonResolvableInterfaceObjects);
    }

    [Fact]
    public void MergeInto_Should_PreserveExistingCompatibility_When_OverrideIsUnset()
    {
        // arrange
        var existing = new CompositionSettings
        {
            ApolloFederationCompatibility =
            {
                AllowNonResolvableInterfaceObjects = true
            }
        };

        // act
        var merged = new CompositionSettings().MergeInto(existing);

        // assert
        Assert.True(merged.ApolloFederationCompatibility.AllowNonResolvableInterfaceObjects);
    }

    [Fact]
    public void NodeResolution_Should_RoundTripAsString_When_Serialized()
    {
        var settings = new CompositionSettings
        {
            Merger =
            {
                EnableGlobalObjectIdentification = true,
                NodeResolution = NodeResolution.SourceSchema
            }
        };

        using var document = JsonSerializer.SerializeToDocument(
            settings,
            SettingsJsonSerializerContext.Default.CompositionSettings);
        var roundTripped = document.Deserialize(
            SettingsJsonSerializerContext.Default.CompositionSettings);

        Assert.Equal(
            "SourceSchema",
            document.RootElement
                .GetProperty("merger")
                .GetProperty("nodeResolution")
                .GetString());
        Assert.Equal(NodeResolution.SourceSchema, roundTripped!.Merger.NodeResolution);
        Assert.Equal(NodeResolution.SourceSchema, roundTripped.Merger.ToOptions().NodeResolution);
    }

    [Fact]
    public void MergeInto_Should_PreserveExistingNodeResolution_When_OverrideIsUnset()
    {
        var existing = new CompositionSettings
        {
            Merger = { NodeResolution = NodeResolution.SourceSchema }
        };

        var merged = new CompositionSettings().MergeInto(existing);

        Assert.Equal(NodeResolution.SourceSchema, merged.Merger.NodeResolution);
    }

    [Fact]
    public void LegacySettings_Should_DefaultNodeResolutionToGateway()
    {
        const string json =
            """
            {
              "merger": {
                "enableGlobalObjectIdentification": true
              }
            }
            """;

        var settings = JsonSerializer.Deserialize(
            json,
            SettingsJsonSerializerContext.Default.CompositionSettings);

        Assert.Null(settings!.Merger.NodeResolution);
        Assert.Equal(NodeResolution.Gateway, settings.Merger.ToOptions().NodeResolution);
    }
}
