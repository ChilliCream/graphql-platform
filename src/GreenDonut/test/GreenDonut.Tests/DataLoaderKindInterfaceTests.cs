namespace GreenDonut;

public class DataLoaderKindInterfaceTests
{
    [Theory]
    [InlineData(typeof(BatchDataLoader<string, string>))]
    [InlineData(typeof(StatefulBatchDataLoader<string, string>))]
    public void Type_ShouldImplementBatchInterface_WhenItIsABatchDataLoader(Type type)
    {
        // arrange
        // act
        var interfaces = type.GetInterfaces();

        // assert
        Assert.Contains(typeof(IBatchDataLoader<string, string>), interfaces);
    }

    [Theory]
    [InlineData(typeof(CacheDataLoader<string, string>))]
    [InlineData(typeof(StatefulCacheDataLoader<string, string>))]
    public void Type_ShouldImplementCacheInterface_WhenItIsACacheDataLoader(Type type)
    {
        // arrange
        // act
        var interfaces = type.GetInterfaces();

        // assert
        Assert.Contains(typeof(ICacheDataLoader<string, string>), interfaces);
    }
}
