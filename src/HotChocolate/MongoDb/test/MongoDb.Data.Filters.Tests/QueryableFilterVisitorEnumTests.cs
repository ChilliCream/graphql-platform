using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Xunit;
using Squadron;

namespace HotChocolate.MongoDb.Data.Filters
{
    public class QueryableFilterVisitorEnumTests
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

        public QueryableFilterVisitorEnumTests(MongoResource resource)
        {
            Init(resource);
        }

        [Fact]
        public async Task Create_EnumEqual_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: BAR } }) { barEnum } }")
                    .Create());

            res1.MatchDocumentSnapshot("BAR");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: FOO } }) { barEnum } }")
                    .Create());

            res2.MatchDocumentSnapshot("FOO");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: null } }) { barEnum } }")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_EnumNotEqual_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: BAR } }) { barEnum } }")
                    .Create());

            res1.MatchDocumentSnapshot("BAR");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: FOO } }) { barEnum } }")
                    .Create());

            res2.MatchDocumentSnapshot("FOO");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: null } }){ barEnum } }")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_EnumIn_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ BAR FOO ]}}){ barEnum}}")
                    .Create());

            res1.MatchDocumentSnapshot("BarAndFoo");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ FOO ]}}){ barEnum}}")
                    .Create());

            res2.MatchDocumentSnapshot("FOO");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ null FOO ]}}){ barEnum}}")
                    .Create());

            res3.MatchDocumentSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_EnumNotIn_Expression()
        {
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ BAR FOO ] } }) { barEnum } }")
                    .Create());

            res1.MatchDocumentSnapshot("BarAndFoo");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ FOO ] } }) { barEnum } }")
                    .Create());

            res2.MatchDocumentSnapshot("FOO");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ null FOO ] } }) { barEnum } }")
                    .Create());

            res3.MatchDocumentSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_NullableEnumEqual_Expression()
        {
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: BAR } }) { barEnum } }")
                    .Create());

            res1.MatchDocumentSnapshot("BAR");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: FOO } }) { barEnum } }")
                    .Create());

            res2.MatchDocumentSnapshot("FOO");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { eq: null } }){ barEnum } }")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableEnumNotEqual_Expression()
        {
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: BAR } }) { barEnum } }")
                    .Create());

            res1.MatchDocumentSnapshot("BAR");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: FOO } }) { barEnum } }")
                    .Create());

            res2.MatchDocumentSnapshot("FOO");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { neq: null } }) { barEnum } }")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableEnumIn_Expression()
        {
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ BAR FOO ] } }) { barEnum } }")
                    .Create());

            res1.MatchDocumentSnapshot("BarAndFoo");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ FOO ] } }) { barEnum } }")
                    .Create());

            res2.MatchDocumentSnapshot("FOO");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { in: [ null FOO ] } }) { barEnum } }")
                    .Create());

            res3.MatchDocumentSnapshot("nullAndFoo");
        }

        [Fact]
        public async Task Create_NullableEnumNotIn_Expression()
        {
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ BAR FOO ] } }){ barEnum } }")
                    .Create());

            res1.MatchDocumentSnapshot("BarAndFoo");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ FOO ] } }) { barEnum } }")
                    .Create());

            res2.MatchDocumentSnapshot("FOO");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { barEnum: { nin: [ null FOO ] } }) { barEnum } }")
                    .Create());

            res3.MatchDocumentSnapshot("nullAndFoo");
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
