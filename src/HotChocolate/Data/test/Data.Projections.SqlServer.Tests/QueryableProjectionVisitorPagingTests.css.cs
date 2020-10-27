using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate.Data.Projections
{
    public class QueryableProjectionVisitorPagingTests
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = true, Baz = "a" }, new Foo { Bar = false, Baz = "b" }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = true, Baz = "a" },
            new FooNullable { Bar = null, Baz = null },
            new FooNullable { Bar = false, Baz = "c" }
        };

        private readonly SchemaCache _cache = new SchemaCache();

        [Fact]
        public async Task Create_ProjectsTwoProperties_Nodes()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes { bar baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_Nodes()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes { baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsTwoProperties_Edges()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ edges { node { bar baz }} }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_Edges()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ edges { node { baz }} }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsTwoProperties_EdgesAndNodes()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes{ baz } edges { node { bar }} }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_ProjectsOneProperty_EdgesAndNodesOverlap()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes{ baz } edges { node { baz }} }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateNullable_ProjectsTwoProperties_Nodes()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooNullableEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes { bar baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateNullable_ProjectsOneProperty_Nodes()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooNullableEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes { baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateNullable_ProjectsTwoProperties_Edges()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooNullableEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ edges { node { bar baz }} }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateNullable_ProjectsOneProperty_Edges()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooNullableEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ edges { node { baz }} }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateNullable_ProjectsTwoProperties_EdgesAndNodes()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooNullableEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes{ baz } edges { node { bar }} }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateNullable_ProjectsOneProperty_EdgesAndNodesOverlap()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooNullableEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes{ baz } edges { node { baz }} }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_Projection_Should_Stop_When_UseProjectionEncountered()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes{ bar list { barBaz } } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task Create_Projection_Should_Stop_When_UsePagingEncountered()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                usePaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ nodes{ bar paging { nodes {barBaz }} } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateOffsetPaging_ProjectsTwoProperties_Items_WithArgs()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                useOffsetPaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(take:10, skip:1){ items { bar baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateOffsetPaging_ProjectsTwoProperties_Items()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                useOffsetPaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ items { bar baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateOffsetPaging_ProjectsOneProperty_Items()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                useOffsetPaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ items { baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }


        [Fact]
        public async Task CreateOffsetPagingNullable_ProjectsTwoProperties_Items()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooNullableEntities,
                useOffsetPaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ items { bar baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateOffsetPagingNullable_ProjectsOneProperty_Items()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooNullableEntities,
                useOffsetPaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ items { baz } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateOffsetPaging_Projection_Should_Stop_When_UseProjectionEncountered()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                useOffsetPaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ items{ bar list { barBaz } } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        [Fact]
        public async Task CreateOffsetPaging_Projection_Should_Stop_When_UsePagingEncountered()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(
                _fooEntities,
                useOffsetPaging: true);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root{ items{ bar paging { nodes {barBaz }} } }}")
                    .Create());

            res1.MatchSqlSnapshot();
        }

        public class Foo
        {
            public int Id { get; set; }

            public bool Bar { get; set; }

            public string Baz { get; set; }

            public string? Qux { get; set; }

            [UseProjection]
            [SubData]
            public List<Bar> List { get; set; }

            [UsePaging]
            [UseProjection]
            [SubData]
            public List<Bar> Paging { get; set; }
        }

        public class Bar
        {
            public int Id { get; set; }

            public string? BarBaz { get; set; }

            public string? BarQux { get; set; }
        }

        public class FooNullable
        {
            public int Id { get; set; }

            public bool? Bar { get; set; }

            public string? Baz { get; set; }
        }

        public class SubDataAttribute : ObjectFieldDescriptorAttribute
        {
            public override void OnConfigure(
                IDescriptorContext context,
                IObjectFieldDescriptor descriptor,
                MemberInfo member)
            {
                descriptor.Resolver(
                    new List<Bar>
                    {
                        new Bar { BarBaz = "a_a", BarQux = "a_c" },
                        new Bar { BarBaz = "a_b", BarQux = "a_d" }
                    });
            }
        }
    }
}
