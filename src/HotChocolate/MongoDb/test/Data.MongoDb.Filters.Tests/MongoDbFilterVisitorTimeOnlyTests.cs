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
    public class MongoDbFilterVisitorTimeOnlyTests
        : SchemaCache
        , IClassFixture<MongoResource>
    {
#if NET6_0_OR_GREATER
        private static readonly Foo[] _fooEntities =
        {
            new Foo { Bar = new TimeOnly(06, 30) },
            new Foo { Bar = new TimeOnly(16, 00) }
        };

        private static readonly FooNullable[] _fooNullableEntities =
        {
            new FooNullable { Bar = new TimeOnly(06, 30) },
            new FooNullable { Bar = null },
            new FooNullable { Bar = new TimeOnly(16, 00) }
        };

        public MongoDbFilterVisitorTimeOnlyTests(MongoResource resource)
        {
            Init(resource);

            // NOTE: At the time of coding, MongoDB C# Driver doesn't natively support TimeOnly
            BsonSerializer.RegisterSerializationProvider(new LocalTimeOnlySerializationProvider());
        }

        [Fact]
        public async Task Create_TimeOnlyEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"06:30:00\" } }){ bar } }")
                    .Create());

            res1.MatchDocumentSnapshot("0630");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"16:00:00\" } }){ bar } }")
                    .Create());

            res2.MatchDocumentSnapshot("1600");
        }

        [Fact]
        public async Task Create_TimeOnlyNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"06:30:00\" } }){ bar } }")
                    .Create());

            res1.MatchDocumentSnapshot("0630");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"16:00:00\" } }){ bar } }")
                    .Create());

            res2.MatchDocumentSnapshot("1600");
        }

        [Fact]
        public async Task Create_NullableTimeOnlyEqual_Expression()
        {
            // arrange
            IRequestExecutor? tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"06:30:00\" } }){ bar } }")
                    .Create());

            res1.MatchDocumentSnapshot("0630");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: \"16:00:00\" } }){ bar } }")
                    .Create());

            res2.MatchDocumentSnapshot("1600");

            IExecutionResult res3 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { eq: null } }){ bar } }")
                    .Create());

            res3.MatchDocumentSnapshot("null");
        }

        [Fact]
        public async Task Create_NullableTimeOnlyNotEqual_Expression()
        {
            // arrange
            IRequestExecutor tester = CreateSchema<FooNullable, FooNullableFilterType>(
                _fooNullableEntities);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"06:30:00\" } }){ bar } }")
                    .Create());

            res1.MatchDocumentSnapshot("0630");

            IExecutionResult res2 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ root(where: { bar: { neq: \"16:00:00\" } }){ bar } }")
                    .Create());

            res2.MatchDocumentSnapshot("1600");

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

            public TimeOnly Bar { get; set; }
        }

        public class FooNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public TimeOnly? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }

        internal class LocalTimeOnlySerializationProvider : IBsonSerializationProvider
        {
            public IBsonSerializer? GetSerializer(Type type)
            {
                return type == typeof(TimeOnly) ? new TimeOnlySerializer() : null;
            }
        }

        internal class TimeOnlySerializer : StructSerializerBase<TimeOnly>
        {
            public override void Serialize(
                BsonSerializationContext context,
                BsonSerializationArgs args,
                TimeOnly value)
            {
                DateTime dateTime = default(DateTime).Add(value.ToTimeSpan());
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                var ticks = BsonUtils.ToMillisecondsSinceEpoch(dateTime);
                context.Writer.WriteDateTime(ticks);
            }

            public override TimeOnly Deserialize(
                BsonDeserializationContext context,
                BsonDeserializationArgs args)
            {
                long ticks = context.Reader.ReadDateTime();
                DateTime dateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(ticks);
                return new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second);
            }
        }
#endif
    }
}
