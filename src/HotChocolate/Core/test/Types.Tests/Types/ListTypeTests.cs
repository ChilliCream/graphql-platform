using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class ListTypeTests
{
    [Fact]
    public void EnsureElementTypeIsCorrectlySet()
    {
        // arrange
        var innerType = new StringType();

        // act
        var type = new ListType(innerType);

        // assert
        Assert.Equal(innerType, type.ElementType);
    }

    [Fact]
    public void EnsureNonNullElementTypeIsCorrectlySet()
    {
        // arrange
        var innerType = new NonNullType(new StringType());

        // act
        var type = new ListType(innerType);

        // assert
        Assert.Equal(innerType, type.ElementType);
    }

    [Fact]
    public void EnsureNativeTypeIsCorrectlyDetected()
    {
        // arrange
        var innerType = new NonNullType(new StringType());
        var type = new ListType(innerType);

        // act
        var clrType = type.RuntimeType;

        // assert
        Assert.Equal(typeof(List<string>), clrType);
    }

    [Fact]
    public async Task Integration_List_ListValues_Scalars()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ scalars(values: [1,2]) }")
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Integration_List_ScalarValue_Scalars()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ scalars(values: 1) }")
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Integration_List_ListValues_Object()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ objects(values: [{ bar: 1 }, { bar: 2 }]) { bar } }")
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task Integration_List_ScalarValue_Object()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("{ objects(values: { bar: 1 }) { bar } }")
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Query
    {
        public int[] Scalars(int[] values) => values;

        public Foo[] Objects(Foo[] values) => values;
    }

    public class Foo
    {
        public int Bar { get; set; }
    }
}
