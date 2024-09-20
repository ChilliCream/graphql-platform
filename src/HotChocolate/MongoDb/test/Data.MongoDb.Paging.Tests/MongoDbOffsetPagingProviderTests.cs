using HotChocolate.Data.MongoDb;
using HotChocolate.Data.MongoDb.Paging;
using HotChocolate.Types.Descriptors;
using MongoDB.Driver;

namespace HotChocolate.Data;

public class MongoDbOffsetPagingProviderTests
{
    [Theory]
    [InlineData(nameof(AggregateFluent), true)]
    [InlineData(nameof(FindFluent), true)]
    [InlineData(nameof(MongoCollection), true)]
    [InlineData(nameof(IMongoDbExecutable), true)]
    [InlineData(nameof(IExecutable), false)]
    [InlineData(nameof(IQueryable), false)]
    public void CanHandle_MethodReturnType_MatchesResult(string methodName, bool expected)
    {
        // arrange
        var provider = new MongoDbOffsetPagingProvider();
        var member = typeof(MongoDbOffsetPagingProviderTests).GetMethod(methodName)!;
        var type = new DefaultTypeInspector().GetReturnType(member);

        // act
        var result = provider.CanHandle(type);

        // assert
        Assert.Equal(expected, result);
    }

    public IAggregateFluent<Foo> AggregateFluent() =>
        throw new InvalidOperationException();

    public IFindFluent<Foo, Foo> FindFluent() =>
        throw new InvalidOperationException();

    public IMongoCollection<Foo> MongoCollection() =>
        throw new InvalidOperationException();

    public IMongoDbExecutable IMongoDbExecutable() =>
        throw new InvalidOperationException();

    public IExecutable IExecutable() =>
        throw new InvalidOperationException();

    public IQueryable<Foo> IQueryable() =>
        throw new InvalidOperationException();

    public class Foo
    {
    }
}
