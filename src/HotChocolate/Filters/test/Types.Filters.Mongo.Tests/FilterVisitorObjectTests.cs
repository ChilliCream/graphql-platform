using HotChocolate.Language;
using HotChocolate.Types.Filters.Mongo;
using HotChocolate.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitorObjectTests
        : TypeTestBase, IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public FilterVisitorObjectTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public void Create_ObjectStringEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("fooNested",
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
            collectionA.InsertOne(new Foo { FooNested = new FooNested { Bar = "a" } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { FooNested = new FooNested { Bar = "b" } });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_ObjectStringEqualDeep_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));

            EvenDeeperFilterType fooType = CreateType(new EvenDeeperFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<EvenDeeper> collectionA = database.GetCollection<EvenDeeper>("a");
            IMongoCollection<EvenDeeper> collectionB = database.GetCollection<EvenDeeper>("b");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "a" } } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "b" } } });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_ObjectStringEqualRecursive_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("nested",
                    new ObjectValueNode(
                    new ObjectFieldNode("nested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));

            FilterInputType<Recursive> fooType = CreateType(new FilterInputType<Recursive>());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<Recursive> collectionA = database.GetCollection<Recursive>("a");
            IMongoCollection<Recursive> collectionB = database.GetCollection<Recursive>("b");


            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            collectionA.InsertOne(new Recursive { Nested = new Recursive { Nested = new Recursive { Bar = "a" } } });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Recursive { Nested = new Recursive { Nested = new Recursive { Bar = "b" } } });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_ObjectStringEqualNull_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")))))));

            EvenDeeperFilterType fooType = CreateType(new EvenDeeperFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<EvenDeeper> collectionA = database.GetCollection<EvenDeeper>("a");
            IMongoCollection<EvenDeeper> collectionB = database.GetCollection<EvenDeeper>("b");
            IMongoCollection<EvenDeeper> collectionC = database.GetCollection<EvenDeeper>("c");
            IMongoCollection<EvenDeeper> collectionD = database.GetCollection<EvenDeeper>("d");


            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new EvenDeeper { Foo = null });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = null } });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = null } } });
            Assert.False(collectionC.Find(query).Any());

            collectionD.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "a" } } });
            Assert.True(collectionD.Find(query).Any());
        }

        [Fact]
        public void Create_ObjectNull_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        NullValueNode.Default
                    )
                )
            )
            );

            EvenDeeperFilterType fooType = CreateType(new EvenDeeperFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<EvenDeeper> collectionA = database.GetCollection<EvenDeeper>("a");
            IMongoCollection<EvenDeeper> collectionB = database.GetCollection<EvenDeeper>("b");
            IMongoCollection<EvenDeeper> collectionC = database.GetCollection<EvenDeeper>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new EvenDeeper { Foo = null });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = null } });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = null } } });
            Assert.False(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ObjectStringEqualNullWithMultipleFilters_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new ObjectValueNode(
                    new ObjectFieldNode("fooNested",
                        new ObjectValueNode(
                            new ObjectFieldNode("bar",
                                new StringValueNode("a")),
                            new ObjectFieldNode("bar_not",
                                new StringValueNode("c")))))));

            EvenDeeperFilterType fooType = CreateType(new EvenDeeperFilterType());
            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<EvenDeeper> collectionA = database.GetCollection<EvenDeeper>("a");
            IMongoCollection<EvenDeeper> collectionB = database.GetCollection<EvenDeeper>("b");
            IMongoCollection<EvenDeeper> collectionC = database.GetCollection<EvenDeeper>("c");
            IMongoCollection<EvenDeeper> collectionD = database.GetCollection<EvenDeeper>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new EvenDeeper { Foo = null });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = null } });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = null } } });
            Assert.False(collectionC.Find(query).Any());

            collectionD.InsertOne(new EvenDeeper { Foo = new Foo { FooNested = new FooNested { Bar = "a" } } });
            Assert.True(collectionD.Find(query).Any());
        }

        public class EvenDeeper
        {
            public ObjectId Id { get; set; }

            public Foo Foo { get; set; }
        }

        public class Foo
        {
            public ObjectId Id { get; set; }

            public FooNested FooNested { get; set; }
        }

        public class FooNested
        {
            public ObjectId Id { get; set; }

            public string Bar { get; set; }
        }

        public class Recursive
        {
            public ObjectId Id { get; set; }
            public Recursive Nested { get; set; }
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Object(t => t.FooNested).AllowObject(x => x.Filter(y => y.Bar));
            }
        }

        public class EvenDeeperFilterType
            : FilterInputType<EvenDeeper>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<EvenDeeper> descriptor)
            {
                descriptor.Object(t => t.Foo).AllowObject(x => x.Object(y => y.FooNested).AllowObject(z => z.Filter(z => z.Bar)));
            }
        }
    }
}
