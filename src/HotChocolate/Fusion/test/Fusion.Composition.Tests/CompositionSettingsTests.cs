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
}
