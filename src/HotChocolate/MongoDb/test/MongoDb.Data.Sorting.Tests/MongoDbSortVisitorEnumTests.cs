using System;
using System.Threading.Tasks;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.MongoDb.Data.Sorting.Expressions
{
    public class MongoDbSortVisitorEnumTests
        : SchemaCache,
          IClassFixture<MongoResource>
    {
        private static readonly Foo[] _fooEntities =
        {
            new Foo { BarEnum = FooEnum.BAR },
            new Foo { BarEnum = FooEnum.BAZ },
            new Foo { BarEnum = FooEnum.FOO },
            new Foo { BarEnum = FooEnum.QUX }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { BarEnum = FooEnum.BAR },
            new FooNullable { BarEnum = FooEnum.BAZ },
            new FooNullable { BarEnum = FooEnum.FOO },
            new FooNullable { BarEnum = null },
            new FooNullable { BarEnum = FooEnum.QUX }
        };

        public MongoDbSortVisitorEnumTests(
            MongoResource resource)
        {
            Init(resource);
        }

        [Fact]
        public async Task Create_Enum_OrderBy()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooSortType>(_fooEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barEnum: ASC}){ barEnum}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barEnum: DESC}){ barEnum}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        [Fact]
        public async Task Create_Enum_OrderBy_Nullable()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableSortType>(
                _fooNullableEntities);

            // act
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barEnum: ASC}){ barEnum}}")
                    .Create());

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(order: { barEnum: DESC}){ barEnum}}")
                    .Create());

            // assert
            res1.MatchDocumentSnapshot("ASC");
            res2.MatchDocumentSnapshot("DESC");
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public FooEnum BarEnum { get; set; }
        }

        public class FooNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public FooEnum? BarEnum { get; set; }
        }

        public enum FooEnum
        {
            FOO,
            BAR,
            BAZ,
            QUX
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
