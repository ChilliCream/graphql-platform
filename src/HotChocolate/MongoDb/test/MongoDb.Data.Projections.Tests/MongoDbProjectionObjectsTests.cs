using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using MongoDB.Bson.Serialization.Attributes;
using Squadron;
using Xunit;

namespace HotChocolate.MongoDb.Data.Projections
{
    public class MongoDbProjectionObjectTests
        : IClassFixture<MongoResource>
    {
        private static readonly BarNullable[] _barWithoutRelation =
        {
            new BarNullable
            {
                Number = 2,
                Foo = new FooNullable
                {
                    BarEnum = BarEnum.BAR,
                    BarShort = 15,
                    NestedObject = new BarNullableDeep
                    {
                        Foo = new FooDeep { BarString = "Foo" }
                    }
                }
            },
            new BarNullable
            {
                Number = 2, Foo = new FooNullable { BarEnum = BarEnum.FOO, BarShort = 14 }
            },
            new BarNullable { Number = 2 }
        };

        private readonly SchemaCache _cache;

        public MongoDbProjectionObjectTests(MongoResource resource)
        {
            _cache = new SchemaCache(resource);
        }

        [Fact]
        public async Task Should_NotInitializeObject_When_ResultOfLeftJoinIsNull()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barWithoutRelation);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        {
                            root {
                                number
                                foo {
                                   barEnum
                                }
                            }
                        }")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task Should_NotInitializeObject_When_ResultOfLeftJoinIsNull_TwoFields()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barWithoutRelation);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        {
                            root {
                                number
                                foo {
                                    id
                                    barEnum
                                }
                            }
                        }")
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        [Fact]
        public async Task Should_NotInitializeObject_When_ResultOfLeftJoinIsNull_Deep()
        {
            // arrange
            IRequestExecutor tester = _cache.CreateSchema(_barWithoutRelation);

            // act
            // assert
            IExecutionResult res1 = await tester.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery(
                        @"
                        {
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
                    .Create());

            res1.MatchDocumentSnapshot();
        }

        public class Foo
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public short BarShort { get; set; }

            public string BarString { get; set; } = string.Empty;

            public BarEnum BarEnum { get; set; }

            public bool BarBool { get; set; }

            public List<BarDeep> ObjectArray { get; set; }

            public BarDeep NestedObject { get; set; }
        }

        public class FooDeep
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public short BarShort { get; set; }

            public string BarString { get; set; } = string.Empty;
        }

        public class FooNullable
        {
            [BsonId]
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
            public Guid Id { get; set; } = Guid.NewGuid();

            public Foo Foo { get; set; }

            public int Number { get; set; }
        }

        public class BarDeep
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public FooDeep Foo { get; set; }
        }

        public class BarNullableDeep
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public FooDeep? Foo { get; set; }

            public int Number { get; set; }
        }

        public class BarNullable
        {
            [BsonId]
            public Guid Id { get; set; } = Guid.NewGuid();

            public FooNullable? Foo { get; set; }

            public int Number { get; set; }
        }

        public enum BarEnum
        {
            FOO,
            BAR,
            BAZ,
            QUX
        }
    }
}
