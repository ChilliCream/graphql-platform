using HotChocolate.Language;
using HotChocolate.Types.Filters.Mongo;
using HotChocolate.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using Snapshooter.Xunit;
using Squadron;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorContextComparableTests
        : TypeTestBase, IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public QueryableFilterVisitorContextComparableTests(
            MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public void Create_ShortEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 12 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 13 });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_ShortNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 13 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());
        }


        [Fact]
        public void Create_ShortGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gt",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 11 });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo { BarShort = 13 });
            Assert.True(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ShortNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gt",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 11 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo { BarShort = 13 });
            Assert.False(collectionC.Find(query).Any());
        }


        [Fact]
        public void Create_ShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gte",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 11 });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo { BarShort = 13 });
            Assert.True(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gte",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 11 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo { BarShort = 13 });
            Assert.False(collectionC.Find(query).Any());
        }



        [Fact]
        public void Create_ShortLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lt",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 11 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo { BarShort = 13 });
            Assert.False(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ShortNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lt",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 11 });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo { BarShort = 13 });
            Assert.True(collectionC.Find(query).Any());
        }


        [Fact]
        public void Create_ShortLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lte",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 11 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo { BarShort = 13 });
            Assert.False(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lte",
                    new IntValueNode(12)));

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
            collectionA.InsertOne(new Foo { BarShort = 11 });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new Foo { BarShort = 13 });
            Assert.True(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_ShortIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_in",
                new ListValueNode(new[]
                {
                    new IntValueNode(13),
                    new IntValueNode(14)
                }))
            );

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
            collectionA.InsertOne(new Foo { BarShort = 13 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_ShortNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_in",
                new ListValueNode(new[] { new IntValueNode(13), new IntValueNode(14) }
                ))
            );

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
            collectionA.InsertOne(new Foo { BarShort = 12 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { BarShort = 13 });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 12 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 13 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = null });
            Assert.False(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 13 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = null });
            Assert.True(collectionC.Find(query).Any());
        }


        [Fact]
        public void Create_NullableShortGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gt",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");
            IMongoCollection<FooNullable> collectionD = database.GetCollection<FooNullable>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 11 });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = 13 });
            Assert.True(collectionC.Find(query).Any());

            collectionD.InsertOne(new FooNullable { BarShort = null });
            Assert.False(collectionD.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gt",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");
            IMongoCollection<FooNullable> collectionD = database.GetCollection<FooNullable>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 11 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = 13 });
            Assert.False(collectionC.Find(query).Any());

            collectionD.InsertOne(new FooNullable { BarShort = null });
            Assert.True(collectionD.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gte",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");
            IMongoCollection<FooNullable> collectionD = database.GetCollection<FooNullable>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 11 });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = 13 });
            Assert.True(collectionC.Find(query).Any());

            collectionD.InsertOne(new FooNullable { BarShort = null });
            Assert.False(collectionD.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gte",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");
            IMongoCollection<FooNullable> collectionD = database.GetCollection<FooNullable>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 11 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = 13 });
            Assert.False(collectionC.Find(query).Any());

            collectionD.InsertOne(new FooNullable { BarShort = null });
            Assert.True(collectionD.Find(query).Any());
        }



        [Fact]
        public void Create_NullableShortLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lt",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");
            IMongoCollection<FooNullable> collectionD = database.GetCollection<FooNullable>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 11 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = 13 });
            Assert.False(collectionC.Find(query).Any());

            collectionD.InsertOne(new FooNullable { BarShort = null });
            Assert.False(collectionD.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lt",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");
            IMongoCollection<FooNullable> collectionD = database.GetCollection<FooNullable>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 11 });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = 13 });
            Assert.True(collectionC.Find(query).Any());

            collectionD.InsertOne(new FooNullable { BarShort = null });
            Assert.True(collectionD.Find(query).Any());
        }


        [Fact]
        public void Create_NullableShortLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lte",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");
            IMongoCollection<FooNullable> collectionD = database.GetCollection<FooNullable>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 11 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.True(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = 13 });
            Assert.False(collectionC.Find(query).Any());

            collectionD.InsertOne(new FooNullable { BarShort = null });
            Assert.False(collectionD.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lte",
                    new IntValueNode(12)));

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");
            IMongoCollection<FooNullable> collectionD = database.GetCollection<FooNullable>("d");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 11 });
            Assert.False(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = 13 });
            Assert.True(collectionC.Find(query).Any());

            collectionD.InsertOne(new FooNullable { BarShort = null });
            Assert.True(collectionD.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_in",
                new ListValueNode(new[]
                {
                    new IntValueNode(13),
                    new IntValueNode(14)
                }))
            );

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 13 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 12 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = null });
            Assert.False(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_NullableShortNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_in",
                new ListValueNode(new[] { new IntValueNode(13), new IntValueNode(14) }
                ))
            );

            FooNullableFilterType fooNullableType = CreateType(new FooNullableFilterType());

            IMongoDatabase database = _mongoResource.CreateDatabase();
            IMongoCollection<FooNullable> collectionA = database.GetCollection<FooNullable>("a");
            IMongoCollection<FooNullable> collectionB = database.GetCollection<FooNullable>("b");
            IMongoCollection<FooNullable> collectionC = database.GetCollection<FooNullable>("c");

            // act
            var filterContext = new MongoFilterVisitorContext(
                fooNullableType,
                MockFilterConvention.Default.GetExpressionDefinition(),
                TypeConversion.Default);

            FilterVisitor<FilterDefinition<BsonDocument>>.Default.Visit(value, filterContext);
            filterContext.TryCreateQuery(out BsonDocument query);

            // assert
            collectionA.InsertOne(new FooNullable { BarShort = 12 });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new FooNullable { BarShort = 13 });
            Assert.False(collectionB.Find(query).Any());

            collectionC.InsertOne(new FooNullable { BarShort = null });
            Assert.True(collectionC.Find(query).Any());
        }

        [Fact]
        public void Overwrite_Comparable_Filter_Type_With_Attribute()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FilterInputType<EntityWithTypeAttribute>());

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Overwrite_Comparable_Filter_Type_With_Descriptor()
        {
            // arrange
            // act
            ISchema schema = CreateSchema(new FilterInputType<Entity>(d =>
                d.Filter(t => t.BarShort).Type<IntType>()));

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Foo
        {
            public ObjectId Id { get; set; }

            public short BarShort { get; set; }

            public int BarInt { get; set; }

            public long BarLong { get; set; }

            public float BarFloat { get; set; }

            public double BarDouble { get; set; }

            public decimal BarDecimal { get; set; }
        }

        public class FooNullable
        {
            public ObjectId Id { get; set; }

            public short? BarShort { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(x => x.BarShort);
                descriptor.Filter(x => x.BarInt);
                descriptor.Filter(x => x.BarLong);
                descriptor.Filter(x => x.BarFloat);
                descriptor.Filter(x => x.BarDouble);
                descriptor.Filter(x => x.BarDecimal);
            }
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Filter(x => x.BarShort);
            }
        }

        public class EntityWithTypeAttribute
        {
            public ObjectId Id { get; set; }

            [GraphQLType(typeof(IntType))]
            public short? BarShort { get; set; }
        }

        public class Entity
        {
            public ObjectId Id { get; set; }

            public short? BarShort { get; set; }
        }
    }
}
