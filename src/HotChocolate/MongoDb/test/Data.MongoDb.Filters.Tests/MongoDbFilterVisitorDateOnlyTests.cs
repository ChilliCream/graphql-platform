using System;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Xunit;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class MongoDbFilterVisitorDateOnlyTests
        : SchemaCache
        , IClassFixture<MongoResource>
    {
#if NET6_0_OR_GREATER
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = new DateOnly(2022, 01, 16) },
            new Foo { Bar = new DateOnly(2022, 01, 15) }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = new DateOnly(2022, 01, 16) },
            new FooNullable { Bar = null },
            new FooNullable { Bar = new DateOnly(2022, 01, 15) }
        };

        public MongoDbFilterVisitorDateOnlyTests(MongoResource resource)
        {
            Init(resource);

            // NOTE: At the time of coding, MongoDB C# Driver doesn't natively support DateOnly
            BsonSerializer.RegisterSerializationProvider(new LocalDateOnlySerializationProvider());
        }

        [Fact]
        public async Task Create_DateOnlyEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"2022-01-16\" } }){ bar } }")
                    .Create());

            res1.MatchDocumentSnapshot("2022-01-16");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"2022-01-15\" } }){ bar } }")
                    .Create());

            res2.MatchDocumentSnapshot("2022-01-15");
        }

        [Fact]
        public async Task Create_DateOnlyNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"2022-01-15\" } }){ bar } }")
                    .Create());

            res1.MatchDocumentSnapshot("2022-01-15");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"2022-01-16\" } }){ bar } }")
                    .Create());

            res2.MatchDocumentSnapshot("2022-01-16");
        }

        [Fact]
        public async Task Create_NullableDateOnlyEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"2022-01-16\" } }){ bar } }")
                    .Create());

            res1.MatchDocumentSnapshot("2022-01-16");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"2022-01-15\" } }){ bar } }")
                    .Create());

            res2.MatchDocumentSnapshot("2022-01-15");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null } }){ bar } }")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableDateOnlyNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"2022-01-15\" } }){ bar } }")
                    .Create());

            res1.MatchDocumentSnapshot("2022-01-15");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"2022-01-16\" } }){ bar } }")
                    .Create());

            res2.MatchDocumentSnapshot("2022-01-16");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: null } }){ bar } }")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public DateOnly Bar { get; set; }
        }

        public class FooNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public DateOnly? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }

        internal class LocalDateOnlySerializationProvider : IBsonSerializationProvider
        {
            public IBsonSerializer? GetSerializer(Type type)
            {
                return type == typeof(DateOnly) ? new DateOnlySerializer() : null;
            }
        }

        internal class DateOnlySerializer : StructSerializerBase<DateOnly>
        {
            private static readonly TimeOnly zeroTimeComponent = new();

            public override void Serialize(
                BsonSerializationContext context,
                BsonSerializationArgs args,
                DateOnly value)
            {
                var dateTime = value.ToDateTime(zeroTimeComponent, DateTimeKind.Utc);
                var ticks = BsonUtils.ToMillisecondsSinceEpoch(dateTime);
                context.Writer.WriteDateTime(ticks);
            }

            public override DateOnly Deserialize(
                BsonDeserializationContext context,
                BsonDeserializationArgs args)
            {
                long ticks = context.Reader.ReadDateTime();
                DateTime dateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(ticks);
                return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
            }
        }
#endif
    }
}
