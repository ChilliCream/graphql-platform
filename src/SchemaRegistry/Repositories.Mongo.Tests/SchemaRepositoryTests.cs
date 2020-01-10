using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Snapshooter.Xunit;
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
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

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
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            Schema retrieved = await repository.GetSchemaAsync(initial.Id);

            // assert
            Assert.Equal(initial.Name, retrieved.Name);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetSchemaByName()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            Schema retrieved = await repository.GetSchemaAsync(initial.Name);

            // assert
            Assert.Equal(initial.Id, retrieved.Id);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetMultipleSchemas()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var a = new Schema("foo1", "bar");
            var b = new Schema("foo2", "bar");
            await schemas.InsertOneAsync(a, options: null, default);
            await schemas.InsertOneAsync(b, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            IReadOnlyDictionary<Guid, Schema> retrieved =
                await repository.GetSchemasAsync(new[] { a.Id, b.Id });

            // assert
            Assert.True(retrieved.ContainsKey(a.Id));
            Assert.True(retrieved.ContainsKey(b.Id));
        }

        [Fact]
        public async Task GetMultipleSchemasByName()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var a = new Schema("foo1", "bar");
            var b = new Schema("foo2", "bar");
            await schemas.InsertOneAsync(a, options: null, default);
            await schemas.InsertOneAsync(b, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            IReadOnlyDictionary<string, Schema> retrieved =
                await repository.GetSchemasAsync(new[] { a.Name, b.Name });

            // assert
            Assert.True(retrieved.ContainsKey(a.Name));
            Assert.True(retrieved.ContainsKey(b.Name));
        }

        [Fact]
        public async Task AddSchema()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var repository = new SchemaRepository(schemas, versions, publishReports);
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
        public async Task AddSchema_DuplicateName()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var repository = new SchemaRepository(schemas, versions, publishReports);
            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            // act
            Func<Task> action = () => repository.AddSchemaAsync(schema);

            // assert
            DuplicateKeyException ex = await Assert.ThrowsAsync<DuplicateKeyException>(action);
            ex.Message.MatchSnapshot();
        }

        [Fact]
        public async Task UpdateSchema()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

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
        public async Task UpdateSchema_DuplicateName()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);
            await schemas.InsertOneAsync(new Schema("bar", "bar"), options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            var updated = new Schema(initial.Id, "bar", "baz");
            Func<Task> action = () => repository.UpdateSchemaAsync(updated);

            // assert
            DuplicateKeyException ex = await Assert.ThrowsAsync<DuplicateKeyException>(action);
            ex.Message.MatchSnapshot();
        }

        [Fact]
        public void EnsureDuplicateTagsLeadToAnError()
        {
            // arrange
            // act
            Action action = () => new SchemaVersion(
                Guid.NewGuid(),
                "bar",
                "baz",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow),
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
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var repository = new SchemaRepository(schemas, versions, publishReports);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                "baz",
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
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var repository = new SchemaRepository(schemas, versions, publishReports);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                "baz",
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
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var repository = new SchemaRepository(schemas, versions, publishReports);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var a = new SchemaVersion(
                schema.Id,
                "bar",
                "baz",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);
            await repository.AddSchemaVersionAsync(a);

            var b = new SchemaVersion(
                schema.Id,
                "baz",
                "bar",
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
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var repository = new SchemaRepository(schemas, versions, publishReports);
            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                "baz",
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
            Assert.Equal(schemaVersion.Hash, retrieved.Hash);
            Assert.Equal(schemaVersion.Tags.Count, retrieved.Tags.Count);
        }

        [Fact]
        public async Task UpdateSchemaVersion()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var repository = new SchemaRepository(schemas, versions, publishReports);
            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                "baz",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);

            await repository.AddSchemaVersionAsync(schemaVersion);

            var updatedSchemaVersion = new SchemaVersion(
                schemaVersion.Id,
                schema.Id,
                "baz",
                "qux",
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);

            // act
            await repository.UpdateSchemaVersionAsync(updatedSchemaVersion);

            // assert
            SchemaVersion retrieved = await versions.AsQueryable()
                .Where(t => t.Id == schemaVersion.Id)
                .FirstOrDefaultAsync();
            Assert.NotNull(retrieved);
            Assert.Equal(schemaVersion.Id, retrieved.Id);
            Assert.Equal(schemaVersion.Published, retrieved.Published, TimeSpan.FromSeconds(1));
            Assert.Equal(schemaVersion.SchemaId, retrieved.SchemaId);
            Assert.Equal(updatedSchemaVersion.SourceText, retrieved.SourceText);
            Assert.Equal(updatedSchemaVersion.Hash, retrieved.Hash);
            Assert.Equal(updatedSchemaVersion.Tags.Count, retrieved.Tags.Count);
        }

        [Fact]
        public async Task GetPublishReports()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id, "foo", "bar", Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);
            await publishReports.InsertOneAsync(initialReport, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            SchemaPublishReport retrieved = repository.GetPublishReports()
                .Where(t => t.Id == initialReport.Id)
                .FirstOrDefault();

            // assert
            Assert.Equal(initialReport.State, retrieved.State);
        }

        [Fact]
        public async Task GetPublishReportBySchemaVersionIdAndEnvironmentId()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id, "foo", "bar", Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);
            await publishReports.InsertOneAsync(initialReport, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            SchemaPublishReport retrieved = await repository.GetPublishReportAsync(
                initialReport.SchemaVersionId, initialReport.EnvironmentId);

            // assert
            Assert.Equal(initialReport.Id, retrieved.Id);
        }

        [Fact]
        public async Task GetPublishReportsById()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id, "foo", "bar", Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);
            await publishReports.InsertOneAsync(initialReport, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            IReadOnlyDictionary<Guid, SchemaPublishReport> retrieved =
                await repository.GetPublishReportsAsync(new[] { initialReport.Id });

            // assert
            Assert.True(retrieved.ContainsKey(initialReport.Id));
        }

        [Fact]
        public async Task AddPublishReport()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id, "foo", "bar", Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            await repository.AddPublishReportAsync(initialReport);

            // assert
            IReadOnlyDictionary<Guid, SchemaPublishReport> retrieved =
                await repository.GetPublishReportsAsync(new[] { initialReport.Id });
            Assert.True(retrieved.ContainsKey(initialReport.Id));
        }

        [Fact]
        public async Task UpdatePublishReport()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id, "foo", "bar", Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);
            await publishReports.InsertOneAsync(initialReport, options: null, default);

            var repository = new SchemaRepository(schemas, versions, publishReports);

            // act
            await repository.UpdatePublishReportAsync(new SchemaPublishReport(
                initialReport.Id, initialReport.SchemaVersionId,
                initialReport.EnvironmentId, initialReport.Issues,
                PublishState.Rejected, DateTime.UtcNow));

            // assert
            IReadOnlyDictionary<Guid, SchemaPublishReport> retrieved =
                await repository.GetPublishReportsAsync(new[] { initialReport.Id });
            Assert.Equal(PublishState.Rejected, retrieved[initialReport.Id].State);
        }
    }
}
