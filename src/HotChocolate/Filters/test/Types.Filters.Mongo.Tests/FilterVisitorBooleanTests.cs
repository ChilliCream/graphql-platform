using HotChocolate.Language;
using HotChocolate.Types.Filters.Mongo;
using HotChocolate.Utilities;
using MongoDB.Bson;
using MongoDB.Driver;
using Squadron;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitorContextBooleanTests
        : TypeTestBase, IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public FilterVisitorContextBooleanTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public void Create_BooleanEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(true)));

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
            collectionA.InsertOne(new Foo { Bar = true });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { Bar = false });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_BooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(false)));

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
            collectionA.InsertOne(new Foo { Bar = false });
            Assert.True(collectionA.Find(query).Any());

            collectionB.InsertOne(new Foo { Bar = true });
            Assert.False(collectionB.Find(query).Any());
        }

        [Fact]
        public void Create_NullableBooleanEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(true)));

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
            collectionA.InsertOne(new FooNullable { Bar = true });
            Assert.True(collectionA.Find(query).Any());

            collectionA.InsertOne(new FooNullable { Bar = false });
            Assert.False(collectionB.Find(query).Any());

            collectionA.InsertOne(new FooNullable { Bar = null });
            Assert.False(collectionC.Find(query).Any());
        }

        [Fact]
        public void Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new BooleanValueNode(false)));

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
            collectionA.InsertOne(new FooNullable { Bar = false });
            Assert.True(collectionA.Find(query).Any());

            collectionA.InsertOne(new FooNullable { Bar = true });
            Assert.False(collectionB.Find(query).Any());

            collectionA.InsertOne(new FooNullable { Bar = null });
            Assert.False(collectionC.Find(query).Any());
        }

        public class Foo
        {
            public ObjectId Id { get; set; }

            public bool Bar { get; set; }
        }

        public class FooNullable
        {
            public ObjectId Id { get; set; }

            public bool? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .AllowEquals().And().AllowNotEquals();
            }
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Filter(t => t.Bar)
                    .AllowEquals().And().AllowNotEquals();
            }
        }
    }
}
