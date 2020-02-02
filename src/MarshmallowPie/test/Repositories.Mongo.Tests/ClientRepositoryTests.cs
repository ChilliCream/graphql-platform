using System;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Squadron;
using Xunit;

namespace MarshmallowPie.Repositories.Mongo
{
    public class ClientRepositoryTests
       : IClassFixture<MongoResource>
    {
        private readonly MongoResource _mongoResource;
        private readonly IMongoCollection<Client> _clients;
        private readonly IMongoCollection<ClientVersion> _versions;
        private readonly IMongoCollection<Query> _queries;
        private readonly IMongoCollection<ClientPublishReport> _publishReports;
        private readonly ClientRepository _repository;

        public ClientRepositoryTests(MongoResource mongoResource)
        {
            _mongoResource = mongoResource;

            _clients = mongoResource.CreateCollection<Client>();
            _versions = mongoResource.CreateCollection<ClientVersion>();
            _queries = mongoResource.CreateCollection<Query>();
            _publishReports = mongoResource.CreateCollection<ClientPublishReport>();
            _repository = new ClientRepository(_clients, _versions, _queries, _publishReports);
        }

        [Fact]
        public async Task GetClients()
        {
            // arrange
            Guid schemaId = Guid.NewGuid();
            var initial = new Client(schemaId, "foo", "bar");
            await _clients.InsertOneAsync(initial, options: null, default);

            // act
            Client retrieved = _repository.GetClients()
                .Where(t => t.Id == initial.Id)
                .Single();

            // assert
            Assert.Equal(initial.Name, retrieved.Name);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetClients_By_SchemaId()
        {
            // arrange
            Guid schemaId = Guid.NewGuid();
            var initial = new Client(schemaId, "foo", "bar");
            await _clients.InsertOneAsync(initial, options: null, default);

            // act
            Client retrieved = _repository.GetClients(schemaId)
                .Where(t => t.Id == initial.Id)
                .Single();

            // assert
            Assert.Equal(initial.Name, retrieved.Name);
            Assert.Equal(initial.Description, retrieved.Description);
        }

        [Fact]
        public async Task GetClients_By_Invalid_SchemaId()
        {
            // arrange
            Guid schemaId = Guid.NewGuid();
            var initial = new Client(schemaId, "foo", "bar");
            await _clients.InsertOneAsync(initial, options: null, default);

            // act
            int count = _repository.GetClients(Guid.NewGuid())
                .Where(t => t.Id == initial.Id)
                .Count();

            // assert
            Assert.Equal(0, count);
        }

    }
}
