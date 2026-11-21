// This code was originally forked of https://github.com/dotnet/aspnetcore/tree/c7aae8ff34dce81132d0fb3a976349dcc01ff903/src/Extensions/Features/src

namespace HotChocolate.Features;

public class FeatureCollectionTests
{
    [Fact]
    public void AddedInterfaceIsReturned()
    {
        var interfaces = new FeatureCollection();
        var thing = new Thing();

        interfaces[typeof(IThing)] = thing;

        var thing2 = interfaces[typeof(IThing)];
        Assert.Equal(thing2, thing);
    }

    [Fact]
    public void IndexerAlsoAddsItems()
    {
        var interfaces = new FeatureCollection();
        var thing = new Thing();

        interfaces[typeof(IThing)] = thing;

        Assert.Equal(interfaces[typeof(IThing)], thing);
    }

    [Fact]
    public void SetNullValueRemoves()
    {
        var interfaces = new FeatureCollection();
        var thing = new Thing();

        interfaces[typeof(IThing)] = thing;
        Assert.Equal(interfaces[typeof(IThing)], thing);

        interfaces[typeof(IThing)] = null;

        var thing2 = interfaces[typeof(IThing)];
        Assert.Null(thing2);
    }

    [Fact]
    public void GetMissingStructFeatureThrows()
    {
        var interfaces = new FeatureCollection();

        // Regression test: Used to throw NullReferenceException because it tried to unbox a null object to a struct
        var ex = Assert.Throws<InvalidOperationException>(() => interfaces.Get<int>());
        Assert.Equal(
            "System.Int32 does not exist in the feature collection and because it is "
            + "a struct the method can't return null. Use 'featureCollection[typeof(System.Int32)] "
            + "is not null' to check if the feature exists.", ex.Message);
    }

    [Fact]
    public void GetMissingFeatureReturnsNull()
    {
        var interfaces = new FeatureCollection();

        Assert.Null(interfaces.Get<Thing>());
    }

    [Fact]
    public void GetStructFeature()
    {
        var interfaces = new FeatureCollection();
        const int value = 20;
        interfaces.Set(value);

        Assert.Equal(value, interfaces.Get<int>());
    }

    [Fact]
    public void GetNullableStructFeatureWhenSetWithNonNullableStruct()
    {
        var interfaces = new FeatureCollection();
        const int value = 20;
        interfaces.Set(value);

        Assert.Null(interfaces.Get<int?>());
    }

    [Fact]
    public void GetNullableStructFeatureWhenSetWithNullableStruct()
    {
        var interfaces = new FeatureCollection();
        const int value = 20;
        interfaces.Set<int?>(value);

        Assert.Equal(value, interfaces.Get<int?>());
    }

    [Fact]
    public void GetFeature()
    {
        var interfaces = new FeatureCollection();
        var thing = new Thing();
        interfaces.Set(thing);

        Assert.Equal(thing, interfaces.Get<Thing>());
    }

    [Fact]
    public void Indexer_Should_ReplaceValue_When_KeyIsSetAgain()
    {
        // arrange
        var features = new FeatureCollection();

        // act
        features[typeof(byte)] = "a";
        features[typeof(short)] = "b";
        features[typeof(byte)] = "c";

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 3
            Byte: c
            Int16: b
            """);
    }

    [Fact]
    public void Indexer_Should_IncrementRevisionOnlyForRemovedFeatures_When_ValueIsNull()
    {
        // arrange
        var features = new FeatureCollection();
        features[typeof(byte)] = "a";
        features[typeof(short)] = "b";
        features[typeof(int)] = "c";

        // act
        features[typeof(short)] = null;
        features[typeof(short)] = null;
        features[typeof(long)] = null;

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 4
            Byte: a
            Int32: c
            """);
    }

    [Fact]
    public void Indexer_Should_KeepInsertionOrder_When_FeaturesAreAddedAfterRemovals()
    {
        // arrange
        var features = new FeatureCollection();
        features[typeof(byte)] = "a";
        features[typeof(short)] = "b";
        features[typeof(int)] = "c";
        features[typeof(long)] = "d";
        features[typeof(byte)] = null;
        features[typeof(int)] = null;

        // act
        features[typeof(float)] = "e";
        features[typeof(double)] = "f";
        features[typeof(decimal)] = "g";
        features[typeof(string)] = "h";

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 10
            Int16: b
            Int64: d
            Single: e
            Double: f
            Decimal: g
            String: h
            """);
    }

    [Fact]
    public void Indexer_Should_ReuseLastSlot_When_LastFeatureWasRemoved()
    {
        // arrange
        var features = new FeatureCollection();
        features[typeof(byte)] = "a";
        features[typeof(short)] = "b";
        features[typeof(int)] = "c";
        features[typeof(int)] = null;

        // act
        features[typeof(long)] = "d";
        features[typeof(float)] = "e";

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 6
            Byte: a
            Int16: b
            Int64: d
            Single: e
            """);
    }

    [Fact]
    public void Indexer_Should_RemoveFeature_When_CollectionHoldsMoreThanFourFeatures()
    {
        // arrange
        var features = new FeatureCollection();
        features[typeof(byte)] = "a";
        features[typeof(short)] = "b";
        features[typeof(int)] = "c";
        features[typeof(long)] = "d";
        features[typeof(float)] = "e";

        // act
        features[typeof(short)] = null;
        features[typeof(float)] = "f";

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 7
            Byte: a
            Int32: c
            Int64: d
            Single: f
            """);
    }

    [Fact]
    public void Indexer_Should_StoreFeatures_When_InitialCapacityExceedsFour()
    {
        // arrange
        var features = new FeatureCollection(16);

        // act
        features[typeof(byte)] = "a";
        features[typeof(short)] = "b";
        features[typeof(int)] = "c";
        features[typeof(byte)] = null;

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 4
            Int16: b
            Int32: c
            """);
    }

    [Fact]
    public void IsEmpty_Should_ReturnTrue_When_AllFeaturesWereRemoved()
    {
        // arrange
        var features = new FeatureCollection();
        features[typeof(byte)] = "a";
        features[typeof(short)] = "b";

        // act
        features[typeof(byte)] = null;
        features[typeof(short)] = null;

        // assert
        Assert.True(features.IsEmpty);
    }

    [Fact]
    public void IsEmpty_Should_ReturnFalse_When_FeatureIsSet()
    {
        // arrange
        var features = new FeatureCollection();

        // act
        features[typeof(byte)] = "a";

        // assert
        Assert.False(features.IsEmpty);
    }

    [Fact]
    public void GetEnumerator_Should_MaskDefaults_When_FeatureIsOverridden()
    {
        // arrange
        var defaults = new FeatureCollection();
        defaults[typeof(byte)] = "default-a";
        defaults[typeof(short)] = "default-b";
        var features = new FeatureCollection(defaults);

        // act
        features[typeof(byte)] = "a";
        features[typeof(int)] = "c";

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 4
            Byte: a
            Int32: c
            Int16: default-b
            """);
    }

    [Fact]
    public void GetEnumerator_Should_ReturnDefaults_When_OverridesWereRemoved()
    {
        // arrange
        var defaults = new FeatureCollection();
        defaults[typeof(byte)] = "default-a";
        defaults[typeof(short)] = "default-b";
        var features = new FeatureCollection(defaults);
        features[typeof(byte)] = "a";

        // act
        features[typeof(byte)] = null;

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 4
            Byte: default-a
            Int16: default-b
            """);
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_FeatureHasOtherTypeAndDefaultsMatch()
    {
        // arrange
        var defaults = new FeatureCollection();
        defaults.Set("default");
        var features = new FeatureCollection(defaults);
        features[typeof(string)] = 42;

        // act
        var found = features.TryGet<string>(out var feature);

        // assert
        Assert.False(found);
        Assert.Null(feature);
    }

    [Fact]
    public void TryGet_Should_ReturnFeature_When_CollectionHoldsMoreThanFourFeatures()
    {
        // arrange
        var features = new FeatureCollection();
        var thing = new Thing();
        features[typeof(byte)] = "a";
        features[typeof(short)] = "b";
        features[typeof(int)] = "c";
        features[typeof(long)] = "d";
        features.Set<IThing>(thing);

        // act
        var found = features.TryGet<IThing>(out var feature);

        // assert
        Assert.True(found);
        Assert.Same(thing, feature);
    }
}
