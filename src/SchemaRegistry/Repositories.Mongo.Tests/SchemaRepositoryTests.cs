using System.Xml.Schema;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Squadron;
using Xunit;
using System.Collections.Generic;
using System;

namespace MarshmallowPie.Repositories.Mongo
{
    public class SchemaRepositoryTests
        : IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public SchemaRepositoryTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public async Task GetSchemas()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(schemas, versions);

            // act
            Schema retrieved = repository.GetSchemas()
                .Where(t => t.Id == initial.Id)
                .FirstOrDefault();

            // assert
            var maps = BsonClassMap.GetRegisteredClassMaps();
            Assert.Equal(initial.Name, retrieved.Name);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetSchema()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(schemas, versions);

            // act
            Schema retrieved = await repository.GetSchemaAsync(initial.Id);

            // assert
            Assert.Equal(initial.Name, retrieved.Name);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetMultipleSchemas()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();

            var a = new Schema("foo1", "bar");
            var b = new Schema("foo2", "bar");
            await schemas.InsertOneAsync(a, options: null, default);
            await schemas.InsertOneAsync(b, options: null, default);

            var repository = new SchemaRepository(schemas, versions);

            // act
            IReadOnlyDictionary<Guid, Schema> retrieved =
                await repository.GetSchemasAsync(new[] { a.Id, b.Id });

            // assert
            Assert.True(retrieved.ContainsKey(a.Id));
            Assert.True(retrieved.ContainsKey(b.Id));
        }

        [Fact]
        public async Task AddSchema()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();


            var repository = new SchemaRepository(schemas, versions);
            var Schema = new Schema("foo", "bar");

            // act
            await repository.AddSchemaAsync(Schema);

            // assert
            Schema retrieved = await schemas.AsQueryable()
                .Where(t => t.Id == Schema.Id)
                .FirstOrDefaultAsync();
            var maps = BsonClassMap.GetRegisteredClassMaps();
            Assert.Equal(Schema.Name, retrieved.Name);
            Assert.Equal(Schema.Description, retrieved.Description);
        }

        [Fact]
        public async Task UpdateSchema()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(schemas, versions);

            // act
            var updated = new Schema(initial.Id, initial.Name, "abc");
            await repository.UpdateSchemaAsync(updated);

            // assert
            Schema retrieved = await schemas.AsQueryable()
                .Where(t => t.Id == initial.Id)
                .FirstOrDefaultAsync();
            var maps = BsonClassMap.GetRegisteredClassMaps();
            Assert.Equal(updated.Name, retrieved.Name);
            Assert.Equal(updated.Description, retrieved.Description);
        }
    }
}
