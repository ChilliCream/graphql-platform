using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types.Filters.Mongo;
using HotChocolate.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitorContextArrayTests
        : TypeTestBase, IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public FilterVisitorContextArrayTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public void Create_ArraySomeStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("element",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            FooSimpleFilterType fooType = CreateType(new FooSimpleFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooSimple> collectionA = database.GetCollection<FooSimple>("a");
            IMongoCollection<FooSimple> collectionB = database.GetCollection<FooSimple>("b");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooSimple { Bar = new[] { "c", "d", "a" } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooSimple { Bar = new[] { "c", "d", "b" } });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_ArrayAnyStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_any",
                            new BooleanValueNode(true)
                        )
            );

            FooSimpleFilterType fooType = CreateType(new FooSimpleFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooSimple> collectionA = database.GetCollection<FooSimple>("a");
            IMongoCollection<FooSimple> collectionB = database.GetCollection<FooSimple>("b");
            IMongoCollection<FooSimple> collectionC = database.GetCollection<FooSimple>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooSimple { Bar = new[] { "c", "d", "a" } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooSimple { Bar = new string[0] });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooSimple { Bar = null });
            Assert.False(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ArraySomeStringEqualWithNull_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("element",
                            new StringValueNode("a")
                        )
                    )
                )
            );

            FooSimpleFilterType fooType = CreateType(new FooSimpleFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooSimple> collectionA = database.GetCollection<FooSimple>("a");
            IMongoCollection<FooSimple> collectionB = database.GetCollection<FooSimple>("b");
            IMongoCollection<FooSimple> collectionC = database.GetCollection<FooSimple>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooSimple { Bar = new[] { "c", null, "a" } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooSimple { Bar = new[] { "c", null, "b" } });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooSimple { Bar = null });
            Assert.False(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ArraySomeObjectStringEqualWithNull_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            FooFilterType fooType = CreateType(new FooFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<Foo> collectionA = database.GetCollection<Foo>("a");
            IMongoCollection<Foo> collectionB = database.GetCollection<Foo>("b");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);


            // assert
            collectionA.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    null,
                    new FooNested { Bar = "a" }
                }
            });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    null,
                    new FooNested { Bar = "b" }
                }
            });
            Assert.False(collectionB.Find(query).Any());
        }
        [Fact]
        public void Create_ArraySomeObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_some",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            FooFilterType fooType = CreateType(new FooFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<Foo> collectionA = database.GetCollection<Foo>("a");
            IMongoCollection<Foo> collectionB = database.GetCollection<Foo>("b");
            IMongoCollection<Foo> collectionC = database.GetCollection<Foo>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);


            // assert
            collectionA.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    null,
                    new FooNested { Bar = null },
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            });
            Assert.True(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ArrayNoneObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_none",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            FooFilterType fooType = CreateType(new FooFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<Foo> collectionA = database.GetCollection<Foo>("a");
            IMongoCollection<Foo> collectionB = database.GetCollection<Foo>("b");
            IMongoCollection<Foo> collectionC = database.GetCollection<Foo>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);


            // assert
            collectionA.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            });
            Assert.True(collectionB.Find(query).Any());
            collectionC.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    null,
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = null },
                    new FooNested { Bar = "b" }
                }
            });
            Assert.True(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ArrayAllObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_all",
                    new ObjectValueNode(
                        new ObjectFieldNode("bar",
                            new StringValueNode("a")))));

            FooFilterType fooType = CreateType(new FooFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<Foo> collectionA = database.GetCollection<Foo>("a");
            IMongoCollection<Foo> collectionB = database.GetCollection<Foo>("b");
            IMongoCollection<Foo> collectionC = database.GetCollection<Foo>("c");
            IMongoCollection<Foo> collectionD = database.GetCollection<Foo>("d");
            IMongoCollection<Foo> collectionE = database.GetCollection<Foo>("e");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);


            // assert
            collectionA.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" }
                }
            });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" }
                }
            });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            });
            Assert.False(collectionC.Find(query).Any());

            collectionD.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            });
            Assert.False(collectionD.Find(query).Any());

            collectionE.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    null,
                    new FooNested { Bar = null },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            });
            Assert.False(collectionE.Find(query).Any());
        }

        [Fact]
        public void Create_ArrayAnyObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_any",
                    new BooleanValueNode(true)
                )
            );

            FooFilterType fooType = CreateType(new FooFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<Foo> collectionA = database.GetCollection<Foo>("a");
            IMongoCollection<Foo> collectionB = database.GetCollection<Foo>("b");
            IMongoCollection<Foo> collectionC = database.GetCollection<Foo>("c");
            IMongoCollection<Foo> collectionD = database.GetCollection<Foo>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);


            // assert
            collectionA.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { FooNested = new FooNested[] { } });
            Assert.False(collectionB.Find(query).Any());
            collectionC.InsertOne(new Foo { FooNested = null });
            Assert.False(collectionC.Find(query).Any());
            collectionD.InsertOne(new Foo { FooNested = new FooNested[] { null } });
            Assert.True(collectionD.Find(query).Any());
        }

        [Fact]
        public void Create_ArrayNotAnyObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested_any",
                    new BooleanValueNode(false)));

            FooFilterType fooType = CreateType(new FooFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<Foo> collectionA = database.GetCollection<Foo>("a");
            IMongoCollection<Foo> collectionB = database.GetCollection<Foo>("b");
            IMongoCollection<Foo> collectionC = database.GetCollection<Foo>("c");
            IMongoCollection<Foo> collectionD = database.GetCollection<Foo>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);


            // assert
            collectionA.InsertOne(new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { FooNested = new FooNested[] { } });
            Assert.True(collectionB.Find(query).Any());
            collectionC.InsertOne(new Foo { FooNested = null });
            Assert.False(collectionC.Find(query).Any());

            collectionD.InsertOne(new Foo { FooNested = new FooNested[] { null } });
            Assert.False(collectionD.Find(query).Any());
        }


        [Fact]
        public void Create_ArraySomeStringEqual_Expression_Null()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_some",
                            NullValueNode.Default));

            FooSimpleFilterType fooType = CreateType(new FooSimpleFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooSimple> collectionA = database.GetCollection<FooSimple>("a");
            IMongoCollection<FooSimple> collectionB = database.GetCollection<FooSimple>("b");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooSimple { Bar = new[] { "c", null, "a" } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooSimple { Bar = new[] { "c", "d", "b" } });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_ArrayNoneStringEqual_Expression_Null()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_none",
                            NullValueNode.Default));

            FooSimpleFilterType fooType = CreateType(new FooSimpleFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooSimple> collectionA = database.GetCollection<FooSimple>("a");
            IMongoCollection<FooSimple> collectionB = database.GetCollection<FooSimple>("b");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooSimple { Bar = new[] { "c", "d", "a" } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooSimple { Bar = new[] { "c", null, "b" } });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_ArrayAllStringEqual_Expression_Null()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar_all",
                            NullValueNode.Default));

            FooSimpleFilterType fooType = CreateType(new FooSimpleFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooSimple> collectionA = database.GetCollection<FooSimple>("a");
            IMongoCollection<FooSimple> collectionB = database.GetCollection<FooSimple>("b");


            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooSimple { Bar = new string[] { null, null, null } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooSimple { Bar = new[] { "c", "d", "b" } });
            Assert.False(collectionB.Find(query).Any());

        }

        public class Foo
        {
            public ObjectId Id { get; set; }

            public IEnumerable<FooNested> FooNested { get; set; }
        }

        public class FooSimple
        {
            public ObjectId Id { get; set; }

            public IEnumerable<string> Bar { get; set; }
        }

        public class FooNested
        {
            public ObjectId Id { get; set; }

            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.List(t => t.FooNested).BindImplicitly();
            }
        }

        public class FooSimpleFilterType
            : FilterInputType<FooSimple>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooSimple> descriptor)
            {
                descriptor.List(t => t.Bar).BindImplicitly();
            }
        }
    }
}
