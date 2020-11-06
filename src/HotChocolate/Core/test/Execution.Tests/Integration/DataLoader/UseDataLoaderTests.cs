using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using HotChocolate.DataLoader;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution.Integration.DataLoader
{
    public class UseDataLoaderTests
    {
        [Fact]
        public void UseDataLoader_Should_ThrowException_When_NotADataLoader()
        {
            // arrange
            // act
            // assert
            SchemaException exception = Assert.Throws<SchemaException>(
                () => SchemaBuilder.New()
                    .AddQueryType<Query>(
                        x => x.BindFieldsExplicitly()
                            .Field(y => y.Single)
                            .UseDataloader(typeof(Foo)))
                    .Create());

            exception.Message.MatchSnapshot();
        }

        [Fact]
        public void UseDataLoader_Schema_BatchDataloader_Single()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Single)
                        .UseDataloader<TestBatchLoader>())
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void UseDataLoader_Schema_BatchDataloader_Many()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Multiple)
                        .UseDataloader<TestBatchLoader>())
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void UseDataLoader_Schema_GroupedDataloader_Single()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Single)
                        .UseDataloader<TestGroupedLoader>())
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void UseDataLoader_Schema_GroupedDataloader_Many()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Multiple)
                        .UseDataloader<TestGroupedLoader>())
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void UseDataLoaderAttribute_Schema_BatchDataloader_Single()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<BatchQuery>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Single))
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void UseDataLoaderAttribute_Schema_BatchDataloader_Many()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<BatchQuery>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Multiple))
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void UseDataLoaderAttribute_Schema_GroupedDataloader_Single()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<GroupedQuery>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Single))
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void UseDataLoaderAttribute_Schema_GroupedDataloader_Many()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<GroupedQuery>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Multiple))
                .Create();

            // act
            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task UseDataLoader_Schema_BatchDataloader_Single_Execute()
        {
            // arrange
            IRequestExecutor executor = SchemaBuilder.New()
                .AddQueryType<Query>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Single)
                        .UseDataloader<TestBatchLoader>())
                .Create()
                .MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.Create(@"{ single { id }}"));

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task UseDataLoader_Schema_BatchDataloader_Multiple_Execute()
        {
            // arrange
            IRequestExecutor executor = SchemaBuilder.New()
                .AddQueryType<Query>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Multiple)
                        .UseDataloader<TestBatchLoader>())
                .Create()
                .MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.Create(@"{ multiple { id }}"));

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task UseDataLoader_Schema_GroupedDataloader_Single_Execute()
        {
            // arrange
            IRequestExecutor executor = SchemaBuilder.New()
                .AddQueryType<Query>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Single)
                        .UseDataloader<TestGroupedLoader>())
                .Create()
                .MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.Create(@"{ single { id }}"));

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task UseDataLoader_Schema_GroupedDataloader_Multiple_Execute()
        {
            // arrange
            IRequestExecutor executor = SchemaBuilder.New()
                .AddQueryType<Query>(
                    x => x.BindFieldsExplicitly()
                        .Field(y => y.Multiple)
                        .UseDataloader<TestGroupedLoader>())
                .Create()
                .MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.Create(@"{ multiple { id }}"));

            // assert
            result.ToJson().MatchSnapshot();
        }

        public class Query
        {
            public int Single { get; } = 1;

            public int[] Multiple { get; } = { 1, 2, 3, 4 };
        }

        public class BatchQuery
        {
            [UseDataLoader(typeof(TestBatchLoader))]
            public int Single { get; } = 1;

            [UseDataLoader(typeof(TestBatchLoader))]
            public int[] Multiple { get; } = { 1, 2, 3, 4 };
        }

        public class GroupedQuery
        {
            [UseDataLoader(typeof(TestGroupedLoader))]
            public int Single { get; } = 1;

            [UseDataLoader(typeof(TestGroupedLoader))]
            public int[] Multiple { get; } = { 1, 2, 3, 4 };
        }

        public class TestGroupedLoader : GroupedDataLoader<int, Foo>
        {
            public TestGroupedLoader(
                IBatchScheduler batchScheduler,
                DataLoaderOptions<int> options = null) : base(batchScheduler, options)
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
                DataLoaderOptions<int>? options = null) : base(batchScheduler, options)
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
}
