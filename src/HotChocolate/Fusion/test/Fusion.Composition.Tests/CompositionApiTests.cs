using HotChocolate.Fusion.ApolloFederation;

namespace HotChocolate.Fusion;

public sealed class CompositionApiTests
{
    [Fact]
    public void CompositionEngineTypes_Should_NotBeExported_When_InspectingAssembly()
    {
        Type[] engineTypes =
        [
            typeof(SchemaComposer),
            typeof(SourceSchemaText),
            typeof(FederationSchemaTransformer),
            typeof(SettingsComposer)
        ];

        Assert.All(engineTypes, type => Assert.False(type.IsVisible));
    }
}
