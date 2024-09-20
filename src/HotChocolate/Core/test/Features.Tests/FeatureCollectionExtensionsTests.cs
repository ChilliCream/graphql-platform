// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

namespace HotChocolate.Features;

public class FeatureCollectionExtensionsTests
{
    [Fact]
    public void AddedFeatureGetsReturned()
    {
        // Arrange
        var features = new FeatureCollection();
        var thing = new Thing();
        features.Set<IThing>(thing);

        // Act
        var retrievedThing = features.GetRequired<IThing>();

        // Assert
        Assert.NotNull(retrievedThing);
        Assert.Equal(retrievedThing, thing);
    }

    [Fact]
    public void ExceptionThrown_WhenAskedForUnknownFeature()
    {
        // Arrange
        var features = new FeatureCollection();
        var thing = new Thing();
        features.Set<IThing>(thing);

        // Assert
        Assert.Throws<InvalidOperationException>(() => features.GetRequired<object>());
    }
}
