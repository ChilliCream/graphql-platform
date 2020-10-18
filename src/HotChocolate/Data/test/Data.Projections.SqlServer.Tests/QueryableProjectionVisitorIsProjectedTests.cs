using System.Threading.Tasks;
using HotChocolate.Execution;
using Xunit;

namespace HotChocolate.Data.Projections
{
    public class QueryableProjectionVisitorIsProjectedTests
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

        private readonly SchemaCache _cache = new SchemaCache();

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

            res1.MatchSqlSnapshot();
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

            res1.MatchSqlSnapshot();
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

            res1.MatchSqlSnapshot();
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

            res1.MatchSqlSnapshot();
        }

        public class Foo
        {
            public int Id { get; set; }

            [IsProjected(true)]
            public bool? IsProjectedTrue { get; set; }

            [IsProjected(false)]
            public bool? IsProjectedFalse { get; set; }

            public bool? ShouldNeverBeProjected { get; set; }
        }

        public class Bar
        {
            public int Id { get; set; }

            [IsProjected(false)]
            public bool? IsProjectedFalse { get; set; }

            public bool? ShouldNeverBeProjected { get; set; }
        }
    }
}
