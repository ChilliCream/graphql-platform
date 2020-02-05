using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;
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
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var a = new Schema("foo1", "bar");
            var b = new Schema("foo2", "bar");
            await schemas.InsertOneAsync(a, options: null, default);
            await schemas.InsertOneAsync(b, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var a = new Schema("foo1", "bar");
            var b = new Schema("foo2", "bar");
            await schemas.InsertOneAsync(a, options: null, default);
            await schemas.InsertOneAsync(b, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);
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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);
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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);
            await schemas.InsertOneAsync(new Schema("bar", "bar"), options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            var version = new SchemaVersion(
                Guid.NewGuid(),
                "bar",
                new DocumentHash("baz", "baz", HashFormat.Hex),
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow),
                    new Tag("a", "b", DateTime.UtcNow),
                    new Tag("a", "c", DateTime.UtcNow)
                },
                DateTime.UtcNow);

            // assert
            Assert.Collection(version.Tags.OrderBy(t => t.Value),
                tag => Assert.Equal("b", tag.Value),
                tag => Assert.Equal("c", tag.Value));
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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                new DocumentHash("baz", "baz", HashFormat.Hex),
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
            Assert.Equal(schemaVersion.ExternalId, retrieved.ExternalId);
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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                new DocumentHash("baz", "baz", HashFormat.Hex),
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
            Assert.Equal(schemaVersion.ExternalId, retrieved.ExternalId);
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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var a = new SchemaVersion(
                schema.Id,
                "bar",
                new DocumentHash("baz", "baz", HashFormat.Hex),
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);
            await repository.AddSchemaVersionAsync(a);

            var b = new SchemaVersion(
                schema.Id,
                "baz",
                new DocumentHash("baz", "baz", HashFormat.Hex),
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
        public async Task GetSchemaVersion_By_Hash()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                DocumentHash.FromSourceText("bar"),
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);
            await repository.AddSchemaVersionAsync(schemaVersion);

            // act
            SchemaVersion retrieved = await repository.GetSchemaVersionByHashAsync(
                schemaVersion.Hash.Hash);

            // assert
            Assert.NotNull(retrieved);
            Assert.Equal(schemaVersion.Id, retrieved.Id);
            Assert.Equal(schemaVersion.Published, retrieved.Published, TimeSpan.FromSeconds(1));
            Assert.Equal(schemaVersion.SchemaId, retrieved.SchemaId);
            Assert.Equal(schemaVersion.ExternalId, retrieved.ExternalId);
            Assert.Equal(schemaVersion.Tags.Count, retrieved.Tags.Count);
        }

        [Fact]
        public async Task GetSchemaVersion_By_ExternalId()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                DocumentHash.FromSourceText("bar"),
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);
            await repository.AddSchemaVersionAsync(schemaVersion);

            // act
            SchemaVersion retrieved = await repository.GetSchemaVersionByExternalIdAsync(
                schemaVersion.ExternalId);

            // assert
            Assert.NotNull(retrieved);
            Assert.Equal(schemaVersion.Id, retrieved.Id);
            Assert.Equal(schemaVersion.Published, retrieved.Published, TimeSpan.FromSeconds(1));
            Assert.Equal(schemaVersion.SchemaId, retrieved.SchemaId);
            Assert.Equal(schemaVersion.ExternalId, retrieved.ExternalId);
            Assert.Equal(schemaVersion.Tags.Count, retrieved.Tags.Count);
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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);
            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                new DocumentHash("baz", "baz", HashFormat.Hex),
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
            Assert.Equal(schemaVersion.ExternalId, retrieved.ExternalId);
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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);
            var schema = new Schema("foo", "bar");
            await repository.AddSchemaAsync(schema);

            var schemaVersion = new SchemaVersion(
                schema.Id,
                "bar",
                new DocumentHash("baz", "baz", HashFormat.Hex),
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow)
                },
                DateTime.UtcNow);

            await repository.AddSchemaVersionAsync(schemaVersion);

            // act
            await repository.UpdateSchemaVersionTagsAsync(
                schemaVersion.Id,
                new[]
                {
                    new Tag("a", "b", DateTime.UtcNow),
                    new Tag("a", "c", DateTime.UtcNow)
                });

            // assert
            SchemaVersion retrieved = await versions.AsQueryable()
                .Where(t => t.Id == schemaVersion.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(2, retrieved.Tags.Count);
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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id,
                "foo",
                new DocumentHash("baz", "baz", HashFormat.Hex),
                Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id,
                Guid.NewGuid(),
                Array.Empty<Issue>(),
                PublishState.Published,
                DateTime.UtcNow);
            await publishReports.InsertOneAsync(initialReport, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id,
                "foo",
                new DocumentHash("baz", "baz", HashFormat.Hex),
                Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);
            await publishReports.InsertOneAsync(initialReport, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id, "foo",
                DocumentHash.FromSourceText("abc"),
                Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);
            await publishReports.InsertOneAsync(initialReport, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id,
                "foo",
                DocumentHash.FromSourceText("bar"),
                Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            // act
            await repository.SetPublishReportAsync(initialReport);

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
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var initial = new Schema("foo", "bar");
            await schemas.InsertOneAsync(initial, options: null, default);

            var initialVersion = new SchemaVersion(
                initial.Id,
                "foo",
                DocumentHash.FromSourceText("bar"),
                Array.Empty<Tag>(),
                DateTime.UtcNow);
            await versions.InsertOneAsync(initialVersion, options: null, default);

            var initialReport = new SchemaPublishReport(
                initialVersion.Id, Guid.NewGuid(), Array.Empty<Issue>(),
                PublishState.Published, DateTime.UtcNow);
            await publishReports.InsertOneAsync(initialReport, options: null, default);

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            // act
            await repository.SetPublishReportAsync(new SchemaPublishReport(
                initialReport.Id, initialReport.SchemaVersionId,
                initialReport.EnvironmentId, initialReport.Issues,
                PublishState.Rejected, DateTime.UtcNow));

            // assert
            IReadOnlyDictionary<Guid, SchemaPublishReport> retrieved =
                await repository.GetPublishReportsAsync(new[] { initialReport.Id });
            Assert.Equal(PublishState.Rejected, retrieved[initialReport.Id].State);
        }

        [Fact]
        public async Task SetPublishedSchema()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            var initial = new PublishedSchema(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            // act
            await repository.SetPublishedSchemaAsync(initial);

            // assert
            PublishedSchema retrieved =
                await publishedSchemas.AsQueryable().SingleOrDefaultAsync();
            Assert.NotNull(retrieved);
            Assert.Equal(initial.Id, retrieved.Id);
            Assert.Equal(initial.EnvironmentId, retrieved.EnvironmentId);
            Assert.Equal(initial.SchemaId, retrieved.SchemaId);
            Assert.Equal(initial.SchemaVersionId, retrieved.SchemaVersionId);
        }

        [Fact]
        public async Task SetPublishedSchema_Update_Version()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            var initial = new PublishedSchema(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await publishedSchemas.InsertOneAsync(initial);

            // act
            Guid schemaVersionId = Guid.NewGuid();

            await repository.SetPublishedSchemaAsync(
                new PublishedSchema(
                    initial.Id,
                    initial.EnvironmentId,
                    initial.SchemaId,
                    schemaVersionId));

            // assert
            PublishedSchema retrieved =
                await publishedSchemas.AsQueryable().FirstOrDefaultAsync();
            Assert.NotNull(retrieved);
            Assert.Equal(initial.Id, retrieved.Id);
            Assert.Equal(initial.EnvironmentId, retrieved.EnvironmentId);
            Assert.Equal(initial.SchemaId, retrieved.SchemaId);
            Assert.Equal(schemaVersionId, retrieved.SchemaVersionId);
        }

        [Fact]
        public async Task GetPublishedSchema()
        {
            // arrange
            IMongoCollection<Schema> schemas =
                _mongoResource.CreateCollection<Schema>();
            IMongoCollection<SchemaVersion> versions =
                _mongoResource.CreateCollection<SchemaVersion>();
            IMongoCollection<SchemaPublishReport> publishReports =
                _mongoResource.CreateCollection<SchemaPublishReport>();
            IMongoCollection<PublishedSchema> publishedSchemas =
                _mongoResource.CreateCollection<PublishedSchema>();

            var repository = new SchemaRepository(
                schemas, versions, publishReports, publishedSchemas);

            var initial = new PublishedSchema(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            await publishedSchemas.InsertOneAsync(initial);

            // act

            await repository.GetPublishedSchemaAsync(
                initial.SchemaId, initial.EnvironmentId);

            // assert
            PublishedSchema retrieved =
                await publishedSchemas.AsQueryable().FirstOrDefaultAsync();
            Assert.NotNull(retrieved);
            Assert.Equal(initial.Id, retrieved.Id);
            Assert.Equal(initial.EnvironmentId, retrieved.EnvironmentId);
            Assert.Equal(initial.SchemaId, retrieved.SchemaId);
            Assert.Equal(initial.SchemaVersionId, retrieved.SchemaVersionId);
        }
    }
}
