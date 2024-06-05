using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorTimeOnlyTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = new TimeOnly(06, 30), },
        new() { Bar = new TimeOnly(16, 00), },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = new TimeOnly(06, 30), },
        new() { Bar = null, },
        new() { Bar = new TimeOnly(16, 00), },
    ];

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
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"06:30:00\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"16:00:00\" } }){ bar } }")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "0630"), res2, "1600")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_TimeOnlyNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"06:30:00\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"16:00:00\" } }){ bar } }")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "0630"), res2, "1600")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableTimeOnlyEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"06:30:00\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"16:00:00\" } }){ bar } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: null } }){ bar } }")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "0630"), res2, "1600"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableTimeOnlyNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(_fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"06:30:00\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"16:00:00\" } }){ bar } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: null } }){ bar } }")
                .Build());

        // arrange
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "0630"), res2, "1600"), res3, "null")
            .MatchAsync();
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
            var dateTime = default(DateTime).Add(value.ToTimeSpan());
            dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            var ticks = BsonUtils.ToMillisecondsSinceEpoch(dateTime);
            context.Writer.WriteDateTime(ticks);
        }

        public override TimeOnly Deserialize(
            BsonDeserializationContext context,
            BsonDeserializationArgs args)
        {
            var ticks = context.Reader.ReadDateTime();
            var dateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(ticks);
            return new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second);
        }
    }
}
