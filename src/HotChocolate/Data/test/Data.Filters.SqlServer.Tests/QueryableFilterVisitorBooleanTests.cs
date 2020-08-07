using System;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Snapshooter;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{

    public class QueryableFilterVisitorBooleanTests
        : FilterVisitorTestBase, IClassFixture<SqlServerResource>
    {
        public QueryableFilterVisitorBooleanTests(SqlServerResource resource) : base(resource)
        {
        }

        [Fact]
        public async Task Create_BooleanEqual_Expression()
        {
            // arrange
            Foo[]? entities = CreateEntity(
                new Foo { Bar = true },
                new Foo { Bar = false });

            IRequestExecutor? tester = CreateSchema<Foo, FooFilterType>(entities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                .Create());

            res1.MatchSnapshot(new SnapshotNameExtension("true"));

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                .Create());

            res2.MatchSnapshot(new SnapshotNameExtension("false"));
        }

        [Fact]
        public async Task Create_BooleanNotEqual_Expression()
        {
            // arrange
            Foo[]? entities = CreateEntity(
                new Foo { Bar = true },
                new Foo { Bar = false });

            IRequestExecutor? tester = CreateSchema<Foo, FooFilterType>(entities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                .Create());

            res1.MatchSnapshot(new SnapshotNameExtension("true"));

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                .Create());

            res2.MatchSnapshot(new SnapshotNameExtension("false"));
        }

        [Fact]
        public async Task Create_NullableBooleanEqual_Expression()
        {
            // arrange
            FooNullable[]? entities = CreateEntity(
                new FooNullable { Bar = true },
                new FooNullable { Bar = null },
                new FooNullable { Bar = false });

            IRequestExecutor? tester = CreateSchema<FooNullable, FooNullableFilterType>(entities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: true}}){ bar}}")
                .Create());

            res1.MatchSnapshot(new SnapshotNameExtension("true"));

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: false}}){ bar}}")
                .Create());

            res2.MatchSnapshot(new SnapshotNameExtension("false"));

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { eq: null}}){ bar}}")
                .Create());

            res3.MatchSnapshot(new SnapshotNameExtension("null"));
        }

        [Fact]
        public async Task Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            FooNullable[]? entities = CreateEntity(
                new FooNullable { Bar = true },
                new FooNullable { Bar = null },
                new FooNullable { Bar = false });

            IRequestExecutor? tester = CreateSchema<FooNullable, FooNullableFilterType>(entities);

            // act
            // assert
            IExecutionResult? res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: true}}){ bar}}")
                .Create());

            res1.MatchSnapshot(new SnapshotNameExtension("true"));

            IExecutionResult? res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: false}}){ bar}}")
                .Create());

            res2.MatchSnapshot(new SnapshotNameExtension("false"));

            IExecutionResult? res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                .SetQuery("{ root(where: { bar: { neq: null}}){ bar}}")
                .Create());

            res3.MatchSnapshot(new SnapshotNameExtension("null"));
        }

        public class Foo
        {
            public int Id { get; set; }

            public bool Bar { get; set; }
        }

        public class FooNullable
        {
            public int Id { get; set; }

            public bool? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }
    }
}