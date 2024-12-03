using GreenDonut;
using HotChocolate.Types;

namespace HotChocolate.Execution.Integration.DataLoader;

public class UseDataLoaderTests
{
    [Fact]
    public void UseDataLoader_Should_ThrowException_When_NotADataLoader()
    {
        // arrange
        // act
        var exception =
            Assert.Throws<SchemaException>(
                () => SchemaBuilder.New()
                    .AddQueryType<Query>(x => x
                        .BindFieldsExplicitly()
                        .Field(y => y.Single)
                        .UseDataLoader(typeof(Foo)))
                    .Create());

        // assert
        exception.Message.MatchSnapshot();
    }

    [Fact]
    public void UseDataLoader_Schema_BatchDataloader_Single()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>(x => x
                .BindFieldsExplicitly()
                .Field(y => y.Single)
                .UseDataLoader<TestBatchLoader>())
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void UseDataLoader_Schema_BatchDataloader_Many()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>(x => x
                .BindFieldsExplicitly()
                .Field(y => y.Multiple)
                .UseDataLoader<TestBatchLoader>())
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void UseDataLoader_Schema_GroupedDataloader_Single()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>(x => x
                .BindFieldsExplicitly()
                .Field(y => y.Single)
                .UseDataLoader<TestGroupedLoader>())
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void UseDataLoader_Schema_GroupedDataloader_Many()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Query>(x => x
                .BindFieldsExplicitly()
                .Field(y => y.Multiple)
                .UseDataLoader<TestGroupedLoader>())
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void UseDataLoaderAttribute_Schema_BatchDataloader_Single()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<BatchQuery>(
                x => x.BindFieldsExplicitly()
                    .Field(y => y.Single))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void UseDataLoaderAttribute_Schema_BatchDataloader_Many()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<BatchQuery>(
                x => x.BindFieldsExplicitly()
                    .Field(y => y.Multiple))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void UseDataLoaderAttribute_Schema_GroupedDataloader_Single()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<GroupedQuery>(
                x => x.BindFieldsExplicitly()
                    .Field(y => y.Single))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void UseDataLoaderAttribute_Schema_GroupedDataloader_Many()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<GroupedQuery>(
                x => x.BindFieldsExplicitly()
                    .Field(y => y.Multiple))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task UseDataLoader_Schema_BatchDataloader_Single_Execute()
    {
        // arrange
        var executor = SchemaBuilder.New()
            .AddQueryType<Query>(
                x => x.BindFieldsExplicitly()
                    .Field(y => y.Single)
                    .UseDataLoader<TestBatchLoader>())
            .Create()
            .MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromSourceText("{ single { id }}"));

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task UseDataLoader_Schema_BatchDataloader_Multiple_Execute()
    {
        // arrange
        var executor = SchemaBuilder.New()
            .AddQueryType<Query>(
                x => x.BindFieldsExplicitly()
                    .Field(y => y.Multiple)
                    .UseDataLoader<TestBatchLoader>())
            .Create()
            .MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromSourceText("{ multiple { id }}"));

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task UseDataLoader_Schema_GroupedDataloader_Single_Execute()
    {
        // arrange
        var executor = SchemaBuilder.New()
            .AddQueryType<Query>(
                x => x.BindFieldsExplicitly()
                    .Field(y => y.Single)
                    .UseDataLoader<TestGroupedLoader>())
            .Create()
            .MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromSourceText("{ single { id }}"));

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task UseDataLoader_Schema_GroupedDataloader_Multiple_Execute()
    {
        // arrange
        var executor = SchemaBuilder.New()
            .AddQueryType<Query>(
                x => x.BindFieldsExplicitly()
                    .Field(y => y.Multiple)
                    .UseDataLoader<TestGroupedLoader>())
            .Create()
            .MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequest.FromSourceText("{ multiple { id }}"));

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Query
    {
        public int Single { get; } = 1;

        public int[] Multiple { get; } = [1, 2, 3, 4,];
    }

    public class BatchQuery
    {
        [UseDataLoader(typeof(TestBatchLoader))]
        public int Single { get; } = 1;

        [UseDataLoader(typeof(TestBatchLoader))]
        public int[] Multiple { get; } = [1, 2, 3, 4,];
    }

    public class GroupedQuery
    {
        [UseDataLoader(typeof(TestGroupedLoader))]
        public int Single { get; } = 1;

        [UseDataLoader(typeof(TestGroupedLoader))]
        public int[] Multiple { get; } = [1, 2, 3, 4,];
    }

    public class TestGroupedLoader : GroupedDataLoader<int, Foo>
    {
        public TestGroupedLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions? options = null)
            : base(batchScheduler, options)
        {
        }

        protected override Task<ILookup<int, Foo>> LoadGroupedBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(keys.ToLookup(x => x, x => new Foo(x)));
        }
    }

    public class TestBatchLoader : BatchDataLoader<int, Foo>
    {
        public TestBatchLoader(
            IBatchScheduler batchScheduler,
            DataLoaderOptions options)
            : base(batchScheduler, options)
        {
        }

        protected override Task<IReadOnlyDictionary<int, Foo>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                (IReadOnlyDictionary<int, Foo>)keys.ToDictionary(x => x, x => new Foo(x)));
        }
    }

    public class Foo
    {
        public Foo(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }
}
