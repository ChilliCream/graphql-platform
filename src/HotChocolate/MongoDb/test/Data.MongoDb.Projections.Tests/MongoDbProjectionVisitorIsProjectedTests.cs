using System;
using System.Threading.Tasks;
using HotChocolate.Data;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.Data.MongoDb.Projections
{
    public class MongoDbProjectionVisitorIsProjectedTests
        : IClassFixture<MongoResource>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { IsProjectedTrue = true, IsProjectedFalse = false },
            new Foo { IsProjectedTrue = true, IsProjectedFalse = false }
        };

        private static readonly Bar[] _barEntities =
        {
            new Bar { IsProjectedFalse = false }, new Bar { IsProjectedFalse = false }
        };

        private readonly SchemaCache _cache;

        public MongoDbProjectionVisitorIsProjectedTests(MongoResource resource)
        {
            _cache = new SchemaCache(resource);
        }

        [Fact]
        public async Task IsProjected_Should_NotBeProjectedWhenSelected_When_FalseWithOneProps()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { isProjectedFalse }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task IsProjected_Should_NotBeProjectedWhenSelected_When_FalseWithTwoProps()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { isProjectedFalse isProjectedTrue  }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task IsProjected_Should_AlwaysBeProjectedWhenSelected_When_True()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { isProjectedFalse }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task IsProjected_Should_NotFailWhenSelectionSetSkippedCompletely()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root { isProjectedFalse }}")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            [IsProjected(true)]
            public bool? IsProjectedTrue { get; set; }

            [IsProjected(false)]
            public bool? IsProjectedFalse { get; set; }

            public bool? ShouldNeverBeProjected { get; set; }
        }

        public class Bar
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            [IsProjected(false)]
            public bool? IsProjectedFalse { get; set; }

            public bool? ShouldNeverBeProjected { get; set; }
        }
    }
}
