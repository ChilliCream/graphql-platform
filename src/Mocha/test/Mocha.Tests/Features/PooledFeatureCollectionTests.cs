using Mocha.Features;

namespace Mocha.Tests.Features;

public class PooledFeatureCollectionTests
{
    [Fact]
    public void Initialize_Should_KeepCachedFeaturesInvisible_When_CollectionIsReused()
    {
        // arrange
        var features = new PooledFeatureCollection(new object());
        features.GetOrSet<TestFeature>();
        features.Reset();

        // act
        features.Initialize();

        // assert
        Assert.Null(features.Get<TestFeature>());
        Assert.Null(features[typeof(TestFeature)]);
        Assert.False(features.TryGet<TestFeature>(out _));
        Assert.Empty(features);
        Assert.True(features.IsEmpty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetOrSet_Should_ReuseAndInitializeCachedFeature_When_CollectionIsReused(bool inherit)
    {
        // arrange
        var state = new object();
        var features = new PooledFeatureCollection(state);
        var feature = features.GetOrSet<TestFeature>();
        features.Reset();
        features.Initialize(inherit ? new FeatureCollection() : null);
        var revision = features.Revision;

        // act
        var activated = features.GetOrSet<TestFeature>();

        // assert
        Assert.Same(feature, activated);
        Assert.Equal(2, activated.InitializeCount);
        Assert.Same(state, activated.State);
        Assert.True(features.Revision > revision);
        Assert.Equal([activated], features.Select(pair => pair.Value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void GetOrSet_Should_ReuseCachedFeature_When_UsingAnyOverload(int overload)
    {
        // arrange
        var pooled = new PooledFeatureCollection(new object());
        IFeatureCollection features = pooled;
        var cached = features.GetOrSet<TestFeature>();
        pooled.Reset();
        pooled.Initialize(new FeatureCollection());
        var factoryCalls = 0;

        // act
        var feature = overload switch
        {
            0 => features.GetOrSet<TestFeature>(),
            1 => features.GetOrSet(new TestFeature()),
            2 => features.GetOrSet(() =>
            {
                factoryCalls++;
                return new TestFeature();
            }),
            3 => features.GetOrSet(value =>
            {
                factoryCalls++;
                return value;
            }, new TestFeature()),
            4 => FeatureCollectionExtensions.GetOrSet<TestFeature>(features),
            5 => FeatureCollectionExtensions.GetOrSet(features, new TestFeature()),
            6 => FeatureCollectionExtensions.GetOrSet(features, () =>
            {
                factoryCalls++;
                return new TestFeature();
            }),
            _ => FeatureCollectionExtensions.GetOrSet(features, value =>
            {
                factoryCalls++;
                return value;
            }, new TestFeature())
        };

        // assert
        Assert.Same(cached, feature);
        Assert.Equal(0, factoryCalls);
        Assert.Equal(2, feature.InitializeCount);
    }

    [Fact]
    public void GetOrSet_Should_PreferInheritedFeature_When_LocalFeatureIsCached()
    {
        // arrange
        var parent = new PooledFeatureCollection(new object());
        var inherited = parent.GetOrSet<TestFeature>();
        var features = new PooledFeatureCollection(new object());
        var cached = features.GetOrSet<TestFeature>();
        features.Reset();
        features.Initialize(parent);

        // act
        var feature = features.GetOrSet<TestFeature>();
        features.Reset();

        // assert
        Assert.Same(inherited, feature);
        Assert.Equal(1, cached.InitializeCount);
        Assert.Equal(0, inherited.ResetCount);
        Assert.Same(inherited, parent.Get<TestFeature>());
        Assert.True(features.IsEmpty);
    }

    [Fact]
    public void GetOrSet_Should_CreateLocalFeature_When_ParentFeatureIsCached()
    {
        // arrange
        var parent = new PooledFeatureCollection(new object());
        var cached = parent.GetOrSet<TestFeature>();
        parent.Reset();
        parent.Initialize();
        var state = new object();
        var features = new PooledFeatureCollection(state);
        features.Initialize(parent);

        // act
        var feature = features.GetOrSet<TestFeature>();

        // assert
        Assert.NotSame(cached, feature);
        Assert.Same(state, feature.State);
        Assert.Null(parent.Get<TestFeature>());
        Assert.Equal(1, cached.InitializeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reset_Should_RetainOneReusableFeature_When_RepeatedlyActivated(bool inherit)
    {
        // arrange
        var features = new PooledFeatureCollection(new object());
        var feature = features.GetOrSet<TestFeature>();
        var instances = new List<TestFeature>();

        // act
        for (var i = 0; i < 3; i++)
        {
            features.Set(new object());
            features.Reset();
            features.Initialize(inherit ? new FeatureCollection() : null);
            instances.Add(features.GetOrSet<TestFeature>());
        }

        // assert
        Assert.All(instances, instance => Assert.Same(feature, instance));
        Assert.Equal(4, feature.InitializeCount);
        Assert.Equal(3, feature.ResetCount);
        Assert.Null(features.Get<object>());
    }

    [Fact]
    public void Set_Should_CacheReplacement_When_PreviousFeatureIsDormant()
    {
        // arrange
        var state = new object();
        var features = new PooledFeatureCollection(state);
        features.GetOrSet<TestFeature>();
        features.Reset();
        features.Initialize(new FeatureCollection());
        var replacement = new TestFeature();

        // act
        features.Set(replacement);
        features.Reset();
        features.Initialize();
        var feature = features.GetOrSet<TestFeature>();

        // assert
        Assert.Same(replacement, feature);
        Assert.Same(state, feature.State);
        Assert.Equal(2, feature.InitializeCount);
        Assert.Equal(1, feature.ResetCount);
    }

    public sealed class TestFeature : IPooledFeature
    {
        public object? State { get; private set; }

        public int InitializeCount { get; private set; }

        public int ResetCount { get; private set; }

        public void Initialize(object state)
        {
            State = state;
            InitializeCount++;
        }

        public void Reset()
        {
            State = null;
            ResetCount++;
        }
    }
}
