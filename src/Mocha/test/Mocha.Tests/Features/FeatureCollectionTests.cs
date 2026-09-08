using Mocha.Features;

namespace Mocha.Tests.Features;

public class FeatureCollectionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GetOrSet_Should_AddFeature_When_UsingDefaultInterfaceImplementation(int overload)
    {
        // arrange
        IFeatureCollection features = new FeatureCollection();
        var value = new object();
        var factoryCalls = 0;

        // act
        var feature = overload switch
        {
            0 => features.GetOrSet<object>(),
            1 => features.GetOrSet(value),
            2 => features.GetOrSet(() =>
            {
                factoryCalls++;
                return value;
            }),
            _ => features.GetOrSet(state =>
            {
                factoryCalls++;
                return state;
            }, value)
        };

        // assert
        Assert.Same(feature, features.Get<object>());
        Assert.Same(feature, features.GetOrSet<object>());
        Assert.Equal(overload >= 2 ? 1 : 0, factoryCalls);
        if (overload > 0)
        {
            Assert.Same(value, feature);
        }
    }

    [Fact]
    public void GetOrSet_Should_ReturnExistingFeature_When_CollectionIsReadOnly()
    {
        // arrange
        var mutable = new FeatureCollection();
        var value = new object();
        mutable.Set(value);
        IFeatureCollection features = new ReadOnlyFeatureCollection(mutable);
        var factoryCalls = 0;

        // act
        var feature = features.GetOrSet(() =>
        {
            factoryCalls++;
            return new object();
        });

        // assert
        Assert.Same(value, feature);
        Assert.Equal(0, factoryCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetOrSet_Should_RejectNewFeature_When_CollectionIsReadOnly(bool emptySingleton)
    {
        // arrange
        IFeatureCollection features = emptySingleton
            ? FeatureCollection.Empty
            : new ReadOnlyFeatureCollection(new FeatureCollection());

        // act
        var exception = Record.Exception(() => features.GetOrSet<object>());

        // assert
        Assert.IsType<NotSupportedException>(exception);
        Assert.Null(features.Get<object>());
    }
}
