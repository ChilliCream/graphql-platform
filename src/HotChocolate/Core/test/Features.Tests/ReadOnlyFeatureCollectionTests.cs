namespace HotChocolate.Features;

public class ReadOnlyFeatureCollectionTests
{
    [Fact]
    public void Constructor_Should_CopyFeatures_When_SourceHoldsFewFeatures()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(byte)] = "a";
        source[typeof(short)] = "b";
        source[typeof(int)] = "c";

        // act
        var features = new ReadOnlyFeatureCollection(source);

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 3
            Byte: a
            Int16: b
            Int32: c
            """);
    }

    [Fact]
    public void Constructor_Should_IgnoreLaterChanges_When_SourceIsModified()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(byte)] = "a";
        source[typeof(short)] = "b";
        var features = new ReadOnlyFeatureCollection(source);

        // act
        source[typeof(byte)] = null;
        source[typeof(short)] = "changed";
        source[typeof(int)] = "c";

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 2
            Byte: a
            Int16: b
            """);
    }

    [Fact]
    public void Constructor_Should_KeepInsertionOrder_When_SourceHoldsEightFeatures()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(byte)] = "a";
        source[typeof(short)] = "b";
        source[typeof(int)] = "c";
        source[typeof(long)] = "d";
        source[typeof(float)] = "e";
        source[typeof(double)] = "f";
        source[typeof(decimal)] = "g";
        source[typeof(string)] = "h";

        // act
        var features = new ReadOnlyFeatureCollection(source);

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 8
            Byte: a
            Int16: b
            Int32: c
            Int64: d
            Single: e
            Double: f
            Decimal: g
            String: h
            """);
    }

    [Fact]
    public void Constructor_Should_CopyFeatures_When_SourceHoldsMoreThanEightFeatures()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(byte)] = "a";
        source[typeof(short)] = "b";
        source[typeof(int)] = "c";
        source[typeof(long)] = "d";
        source[typeof(float)] = "e";
        source[typeof(double)] = "f";
        source[typeof(decimal)] = "g";
        source[typeof(string)] = "h";
        source[typeof(char)] = "i";

        // act
        var features = new ReadOnlyFeatureCollection(source);

        // assert
        features.Format(orderByName: true).MatchInlineSnapshot(
            """
            Revision: 9
            Byte: a
            Char: i
            Decimal: g
            Double: f
            Int16: b
            Int32: c
            Int64: d
            Single: e
            String: h
            """);
    }

    [Fact]
    public void Constructor_Should_IncludeDefaults_When_SourceHasDefaults()
    {
        // arrange
        var defaults = new FeatureCollection();
        defaults[typeof(byte)] = "default-a";
        defaults[typeof(short)] = "default-b";
        var source = new FeatureCollection(defaults);
        source[typeof(byte)] = "a";
        source[typeof(int)] = "c";

        // act
        var features = new ReadOnlyFeatureCollection(source);

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
    public void Constructor_Should_CopyFeatures_When_SourceIsPooledFeatureCollection()
    {
        // arrange
        var source = new PooledFeatureCollection(new object());
        source[typeof(byte)] = "a";
        source[typeof(short)] = "b";

        // act
        var features = new ReadOnlyFeatureCollection(source);

        // assert
        features.Format().MatchInlineSnapshot(
            """
            Revision: 2
            Byte: a
            Int16: b
            """);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void Constructor_Should_SealFeatures_When_FeaturesAreSealable(int otherFeatures)
    {
        // arrange
        var source = new FeatureCollection();
        var feature = new SealableFeature();
        var keys = new[]
        {
            typeof(byte), typeof(short), typeof(int), typeof(long), typeof(float),
            typeof(double), typeof(decimal), typeof(string), typeof(char), typeof(bool)
        };

        foreach (var key in keys.Take(otherFeatures))
        {
            source[key] = key.Name;
        }

        source.Set(feature);

        // act
        _ = new ReadOnlyFeatureCollection(source);

        // assert
        Assert.True(feature.IsReadOnly);
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_FeatureHasOtherType()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(string)] = 42;
        var features = new ReadOnlyFeatureCollection(source);

        // act
        var found = features.TryGet<string>(out var feature);

        // assert
        Assert.False(found);
        Assert.Null(feature);
    }

    [Fact]
    public void TryGet_Should_ReturnFeature_When_FeatureExists()
    {
        // arrange
        var source = new FeatureCollection();
        var thing = new Thing();
        source[typeof(byte)] = "a";
        source.Set<IThing>(thing);
        var features = new ReadOnlyFeatureCollection(source);

        // act
        var found = features.TryGet<IThing>(out var feature);

        // assert
        Assert.True(found);
        Assert.Same(thing, feature);
    }

    [Fact]
    public void Indexer_Should_ReturnNull_When_FeatureIsMissing()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(byte)] = "a";
        var features = new ReadOnlyFeatureCollection(source);

        // act
        var feature = features[typeof(short)];

        // assert
        Assert.Null(feature);
    }

    [Fact]
    public void Indexer_Should_ThrowArgumentNullException_When_KeyIsNull()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(byte)] = "a";
        var features = new ReadOnlyFeatureCollection(source);

        // act
        void Action() => _ = features[null!];

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Set_Should_ThrowNotSupportedException_When_CollectionIsReadOnly()
    {
        // arrange
        var source = new FeatureCollection();
        source[typeof(byte)] = "a";
        var features = new ReadOnlyFeatureCollection(source);

        // act
        void Action() => features.Set("b");

        // assert
        Assert.Throws<NotSupportedException>(Action);
    }

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

    private sealed class SealableFeature : ISealable
    {
        public bool IsReadOnly { get; private set; }

        public void Seal() => IsReadOnly = true;
    }
}
