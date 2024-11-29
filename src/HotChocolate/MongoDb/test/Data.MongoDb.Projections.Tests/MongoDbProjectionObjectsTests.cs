using HotChocolate.Execution;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;

namespace HotChocolate.Data.MongoDb.Projections;

public class MongoDbProjectionObjectTests(MongoResource resource) : IClassFixture<MongoResource>
{
    private static readonly BarNullable[] _barWithoutRelation =
    [
        new BarNullable(
            number: 2,
            foo: new FooNullable
            {
                BarEnum = BarEnum.BAR,
                BarShort = 15,
                NestedObject = new BarNullableDeep
                {
                    Foo = new FooDeep
                    {
                        BarString = "Foo",
                    },
                },
            }),
        new BarNullable
        {
            Number = 2, Foo = new FooNullable
            {
                BarEnum = BarEnum.FOO,
                BarShort = 14,
            },
        },
        new BarNullable
        {
            Number = 2,
        },
    ];

    private readonly SchemaCache _cache = new(resource);

    [Fact]
    public async Task Should_NotInitializeObject_When_ResultOfLeftJoinIsNull()
    {
        // arrange
        var tester = _cache.CreateSchema(_barWithoutRelation);

        // act
        var res1 = await tester.ExecuteAsync(
            """
            {
              root {
                number
                foo {
                  barEnum
                }
              }
            }
            """);

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Should_NotInitializeObject_When_ResultOfLeftJoinIsNull_TwoFields()
    {
        // arrange
        var tester = _cache.CreateSchema(_barWithoutRelation);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"{
                        root {
                            number
                            foo {
                                barEnum
                            }
                        }
                    }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    [Fact]
    public async Task Should_NotInitializeObject_When_ResultOfLeftJoinIsNull_Deep()
    {
        // arrange
        var tester = _cache.CreateSchema(_barWithoutRelation);

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"{
                        root {
                            number
                            foo {
                                barEnum
                                nestedObject {
                                    foo {
                                        barString
                                    }
                                }
                            }
                        }
                    }")
                .Build());

        // assert
        await Snapshot
            .Create()
            .AddResult(res1)
            .MatchAsync();
    }

    public class Foo
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;

        public BarEnum BarEnum { get; set; }

        public bool BarBool { get; set; }

        public List<BarDeep> ObjectArray { get; set; } = default!;

        public BarDeep NestedObject { get; set; } = default!;
    }

    public class FooDeep
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short BarShort { get; set; }

        public string BarString { get; set; } = string.Empty;
    }

    public class FooNullable
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public short? BarShort { get; set; }

        public string? BarString { get; set; }

        public BarEnum? BarEnum { get; set; }

        public bool? BarBool { get; set; }

        public List<BarNullableDeep?>? ObjectArray { get; set; }

        public BarNullableDeep? NestedObject { get; set; }
    }

    public class Bar
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Foo Foo { get; set; } = default!;

        public int Number { get; set; }
    }

    public class BarDeep
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public FooDeep Foo { get; set; } = default!;
    }

    public class BarNullableDeep
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public FooDeep? Foo { get; set; }

        public int Number { get; set; }
    }

    public class BarNullable
    {
        public BarNullable() { }

        public BarNullable(int number, FooNullable? foo)
        {
            Number = number;
            Foo = foo;
        }

        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; } = Guid.NewGuid();

        public FooNullable? Foo { get; set; }

        public int Number { get; set; }
    }

    public enum BarEnum
    {
        FOO,
        BAR,
        BAZ,
        QUX,
    }
}
