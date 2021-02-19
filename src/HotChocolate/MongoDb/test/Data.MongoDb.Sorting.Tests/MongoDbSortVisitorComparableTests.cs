using System;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.Data.MongoDb.Sorting
{
    public class MongoDbSortVisitorComparableTests
        : SchemaCache,
          IClassFixture<MongoResource>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { BarShort = 12 },
            new Foo { BarShort = 14 },
            new Foo { BarShort = 13 }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { BarShort = 12 },
            new FooNullable { BarShort = null },
            new FooNullable { BarShort = 14 },
            new FooNullable { BarShort = 13 }
        };

        public MongoDbSortVisitorComparableTests(MongoResource resource)
        {
            Init(resource);
        }

        [Fact]
        public async Task Create_Short_OrderBy()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooSortType>(_fooEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barShort: ASC}){ barShort}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barShort: DESC}){ barShort}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        [Fact]
        public async Task Create_Short_OrderBy_Nullable()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableSortType>(
                _fooNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barShort: ASC}){ barShort}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barShort: DESC}){ barShort}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public short BarShort { get; set; }

            public int BarInt { get; set; }

            public long BarLong { get; set; }

            public float BarFloat { get; set; }

            public double BarDouble { get; set; }

            public decimal BarDecimal { get; set; }
        }

        public class FooNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();
            public short? BarShort { get; set; }
        }

        public class FooSortType
            : SortInputType<Foo>
        {
        }

        public class FooNullableSortType
            : SortInputType<FooNullable>
        {
        }
    }
}
