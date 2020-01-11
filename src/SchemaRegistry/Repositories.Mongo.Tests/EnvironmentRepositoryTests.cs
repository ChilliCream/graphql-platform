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
    public class EnvironmentRepositoryTests
        : IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;

        public EnvironmentRepositoryTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;
        }

        [Fact]
        public async Task GetEnvironments()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Environment> collection =
                _mongoResource.CreateCollection<Environment>();

            var initial = new Environment("foo", "bar");
            await collection.InsertOneAsync(initial, options: null, default);

            var repository = new EnvironmentRepository(collection);

            // act
            Environment retrieved = repository.GetEnvironments()
                .Where(t => t.Id == initial.Id)
                .FirstOrDefault();

            // assert
            Assert.Equal(initial.Name, retrieved.Name);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetEnvironment()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Environment> collection =
                _mongoResource.CreateCollection<Environment>();

            var initial = new Environment("foo", "bar");
            await collection.InsertOneAsync(initial, options: null, default);

            var repository = new EnvironmentRepository(collection);

            // act
            Environment retrieved = await repository.GetEnvironmentAsync(initial.Id);

            // assert
            Assert.Equal(initial.Name, retrieved.Name);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetEnvironmentByName()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Environment> collection =
                _mongoResource.CreateCollection<Environment>();

            var initial = new Environment("foo", "bar");
            await collection.InsertOneAsync(initial, options: null, default);

            var repository = new EnvironmentRepository(collection);

            // act
            Environment retrieved = await repository.GetEnvironmentAsync("foo");

            // assert
            Assert.Equal(initial.Id, retrieved.Id);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetMultipleEnvironments()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Environment> collection =
                _mongoResource.CreateCollection<Environment>();

            var a = new Environment("foo1", "bar");
            var b = new Environment("foo2", "bar");
            await collection.InsertOneAsync(a, options: null, default);
            await collection.InsertOneAsync(b, options: null, default);

            var repository = new EnvironmentRepository(collection);

            // act
            IReadOnlyDictionary<Guid, Environment> retrieved =
                await repository.GetEnvironmentsAsync(new[] { a.Id, b.Id });

            // assert
            Assert.True(retrieved.ContainsKey(a.Id));
            Assert.True(retrieved.ContainsKey(b.Id));
        }

        [Fact]
        public async Task GetMultipleEnvironmentsByNames()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Environment> collection =
                _mongoResource.CreateCollection<Environment>();

            var a = new Environment("foo1", "bar");
            var b = new Environment("foo2", "bar");
            await collection.InsertOneAsync(a, options: null, default);
            await collection.InsertOneAsync(b, options: null, default);

            var repository = new EnvironmentRepository(collection);

            // act
            IReadOnlyDictionary<string, Environment> retrieved =
                await repository.GetEnvironmentsAsync(new[] { a.Name, b.Name });

            // assert
            Assert.True(retrieved.ContainsKey(a.Name));
            Assert.True(retrieved.ContainsKey(b.Name));
        }

        [Fact]
        public async Task AddEnvironment()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Environment> collection =
                _mongoResource.CreateCollection<Environment>();


            var repository = new EnvironmentRepository(collection);
            var environment = new Environment("foo", "bar");

            // act
            await repository.AddEnvironmentAsync(environment);

            // assert
            Environment retrieved = await collection.AsQueryable()
                .Where(t => t.Id == environment.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(environment.Name, retrieved.Name);
            Assert.Equal(environment.Description, retrieved.Description);
        }

        [Fact]
        public async Task UpdateEnvironment()
        {
            // arrange
            var db = new MongoClient();
            IMongoCollection<Environment> collection =
                _mongoResource.CreateCollection<Environment>();

            var initial = new Environment("foo", "bar");
            await collection.InsertOneAsync(initial, options: null, default);

            var repository = new EnvironmentRepository(collection);

            // act
            var updated = new Environment(initial.Id, initial.Name, "abc");
            await repository.UpdateEnvironmentAsync(updated);

            // assert
            Environment retrieved = await collection.AsQueryable()
                .Where(t => t.Id == initial.Id)
                .FirstOrDefaultAsync();
            Assert.Equal(updated.Name, retrieved.Name);
            Assert.Equal(updated.Description, retrieved.Description);
        }
    }
}
