using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
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
            var maps = BsonClassMap.GetRegisteredClassMaps();
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
            var maps = BsonClassMap.GetRegisteredClassMaps();
            Assert.Equal(initial.Name, retrieved.Name);
            Assert.Equal(initial.Description, retrieved.Description);
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
            var maps = BsonClassMap.GetRegisteredClassMaps();
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
            var maps = BsonClassMap.GetRegisteredClassMaps();
            Assert.Equal(updated.Name, retrieved.Name);
            Assert.Equal(updated.Description, retrieved.Description);
        }
    }
}
