namespace HotChocolate.Features;

public class ReadOnlyFeatureCollectionTests
{
    [Fact]
    public void IsEmpty_Should_ReturnFalse_When_SourceHasFeatures()
    {
        // arrange
        var source = new FeatureCollection();
        source.Set<IThing>(new Thing());

        // act
        var features = new ReadOnlyFeatureCollection(source);

        // assert
        Assert.False(features.IsEmpty);
    }

    [Fact]
    public void IsEmpty_Should_ReturnTrue_When_SourceHasNoFeatures()
    {
        // arrange
        var source = new FeatureCollection();

        // act
        var features = new ReadOnlyFeatureCollection(source);

        // assert
        Assert.True(features.IsEmpty);
    }

    [Fact]
    public void IsEmpty_Should_ReturnFalse_When_DefaultsAreReadOnlyWithFeatures()
    {
        // arrange
        var source = new FeatureCollection();
        source.Set<IThing>(new Thing());
        var defaults = new ReadOnlyFeatureCollection(source);

        // act
        var features = new FeatureCollection(defaults);

        // assert
        Assert.False(features.IsEmpty);
    }

    [Fact]
    public void ToReadOnly_Should_KeepDefaults_When_OwnFeaturesWereRemoved()
    {
        // arrange
        var defaults = new FeatureCollection();
        var thing = new Thing();
        defaults.Set<IThing>(thing);
        var features = new FeatureCollection(defaults);
        features.Set(new object());
        features.Set<object>(null);

        // act
        var readOnly = features.ToReadOnly();

        // assert
        Assert.Same(thing, readOnly.Get<IThing>());
    }
}
