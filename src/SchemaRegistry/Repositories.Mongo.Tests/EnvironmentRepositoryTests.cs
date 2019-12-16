using System;
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
        public async Task Foo()
        {
            // arrange
            IMongoCollection<Environment> collection =
                _mongoResource.Client
                    .GetDatabase("Foo")
                    .GetCollection<Environment>("Baz");

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

    }
}
