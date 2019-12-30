using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Squadron;
using Xunit;

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
            var schema = new Schema("foo", "bar");

            // act
            await repository.AddSchemaAsync(schema);

            // assert
            Schema retrieved = await schemas.AsQueryable()
                .Where(t => t.Id == schema.Id)
                .FirstOrDefaultAsync();
            Assert.NotNull(retrieved);
            Assert.Equal(schema.Name, retrieved.Name);
            Assert.Equal(schema.Description, retrieved.Description);
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
            Assert.NotNull(retrieved);
            Assert.Equal(updated.Name, retrieved.Name);
            Assert.Equal(updated.Description, retrieved.Description);
        }

        [Fact]
        public void EnsureDuplicateTagsLeadToAnError()
        {
            // arrange
            // act
            Action action = () => new SchemaVersion(
                Guid.NewGuid(),
                "bar",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow),
                    new Tag("a", "c", DateTime.UtcNow)
                },
                DateTime.UtcNow);

            // assert
            Assert.Equal("tags", Assert.Throws<ArgumentException>(action).ParamName);
        }

        [Fact]
        public async Task GetSchemaVersions()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();

            var repository = new SchemaRepository(schemas, versions);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);
            await repository.AddSchemaVersionAsync(schemaVersion);

            // act
            SchemaVersion retrieved = repository.GetSchemaVersions()
                .Where(t => t.Id == schemaVersion.Id)
                .FirstOrDefault();

            // assert
            Assert.NotNull(retrieved);
            Assert.Equal(schemaVersion.Id, retrieved.Id);
            Assert.Equal(schemaVersion.Published, retrieved.Published, TimeSpan.FromSeconds(1));
            Assert.Equal(schemaVersion.SchemaId, retrieved.SchemaId);
            Assert.Equal(schemaVersion.SourceText, retrieved.SourceText);
            Assert.Equal(schemaVersion.Tags.Count, retrieved.Tags.Count);
        }

        [Fact]
        public async Task GetSchemaVersion()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();

            var repository = new SchemaRepository(schemas, versions);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);
            await repository.AddSchemaVersionAsync(schemaVersion);

            // act
            SchemaVersion retrieved = await repository.GetSchemaVersionAsync(schemaVersion.Id);

            // assert
            Assert.NotNull(retrieved);
            Assert.Equal(schemaVersion.Id, retrieved.Id);
            Assert.Equal(schemaVersion.Published, retrieved.Published, TimeSpan.FromSeconds(1));
            Assert.Equal(schemaVersion.SchemaId, retrieved.SchemaId);
            Assert.Equal(schemaVersion.SourceText, retrieved.SourceText);
            Assert.Equal(schemaVersion.Tags.Count, retrieved.Tags.Count);
        }

        [Fact]
        public async Task GetMultipleSchemaVersions()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();

            var repository = new SchemaRepository(schemas, versions);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var a = new SchemaVersion(
                schema.Id,
                "bar",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);
            await repository.AddSchemaVersionAsync(a);

            var b = new SchemaVersion(
                schema.Id,
                "baz",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);
            await repository.AddSchemaVersionAsync(b);

            // act
            IReadOnlyDictionary<Guid, SchemaVersion> retrieved =
                await repository.GetSchemaVersionsAsync(new[] { a.Id, b.Id });

            // assert
            Assert.True(retrieved.ContainsKey(a.Id));
            Assert.True(retrieved.ContainsKey(b.Id));
        }

        [Fact]
        public async Task AddSchemaVersion()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();

            var repository = new SchemaRepository(schemas, versions);
            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);

            // act
            await repository.AddSchemaVersionAsync(schemaVersion);

            // assert
            SchemaVersion retrieved = await versions.AsQueryable()
                .Where(t => t.Id == schemaVersion.Id)
                .FirstOrDefaultAsync();
            Assert.NotNull(retrieved);
            Assert.Equal(schemaVersion.Id, retrieved.Id);
            Assert.Equal(schemaVersion.Published, retrieved.Published, TimeSpan.FromSeconds(1));
            Assert.Equal(schemaVersion.SchemaId, retrieved.SchemaId);
            Assert.Equal(schemaVersion.SourceText, retrieved.SourceText);
            Assert.Equal(schemaVersion.Tags.Count, retrieved.Tags.Count);
        }
    }
}
