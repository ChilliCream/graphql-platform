using CookieCrumble;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using Squadron;

namespace HotChocolate.Data.MongoDb.Filters;

public class MongoDbFilterVisitorDateOnlyTests
    : SchemaCache
    , IClassFixture<MongoResource>
{
    private static readonly Foo[] _fooEntities =
    [
        new() { Bar = new DateOnly(2022, 01, 16), },
        new() { Bar = new DateOnly(2022, 01, 15), },
    ];

    private static readonly FooNullable[] _fooNullableEntities =
    [
        new() { Bar = new DateOnly(2022, 01, 16), },
        new() { Bar = null, },
        new() { Bar = new DateOnly(2022, 01, 15), },
    ];

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
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"2022-01-16\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"2022-01-15\" } }){ bar } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "2022-01-16"), res2, "2022-01-15")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_DateOnlyNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<Foo, FooFilterType>(_fooEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"2022-01-15\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"2022-01-16\" } }){ bar } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    Snapshot
                        .Create(), res1, "2022-01-16"), res2, "2022-01-15")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableDateOnlyEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(
            _fooNullableEntities);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"2022-01-16\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: \"2022-01-15\" } }){ bar } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { eq: null } }){ bar } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "2022-01-16"), res2, "2022-01-15"), res3, "null")
            .MatchAsync();
    }

    [Fact]
    public async Task Create_NullableDateOnlyNotEqual_Expression()
    {
        // arrange
        var tester = CreateSchema<FooNullable, FooNullableFilterType>(
            _fooNullableEntities);

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"2022-01-15\" } }){ bar } }")
                .Build());

        var res2 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: \"2022-01-16\" } }){ bar } }")
                .Build());

        var res3 = await tester.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ root(where: { bar: { neq: null } }){ bar } }")
                .Build());

        // assert
        await SnapshotExtensions.AddResult(
                SnapshotExtensions.AddResult(
                    SnapshotExtensions.AddResult(
                        Snapshot
                            .Create(), res1, "2022-01-16"), res2, "2022-01-15"), res3, "null")
            .MatchAsync();
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

    public class FooFilterType : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterType : FilterInputType<FooNullable>
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
        private static readonly TimeOnly _zeroTimeComponent = new();

        public override void Serialize(
            BsonSerializationContext context,
            BsonSerializationArgs args,
            DateOnly value)
        {
            var dateTime = value.ToDateTime(_zeroTimeComponent, DateTimeKind.Utc);
            var ticks = BsonUtils.ToMillisecondsSinceEpoch(dateTime);
            context.Writer.WriteDateTime(ticks);
        }

        public override DateOnly Deserialize(
            BsonDeserializationContext context,
            BsonDeserializationArgs args)
        {
            var ticks = context.Reader.ReadDateTime();
            var dateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(ticks);
            return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
        }
    }
}
