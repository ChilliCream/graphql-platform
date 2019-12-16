using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MarshmallowPie.Repositories.Mongo
{
    public class EnvironmentRepository
        : IEnvironmentRepository
    {
        private readonly IMongoCollection<Environment> _environments;

        public EnvironmentRepository(IMongoCollection<Environment> environments)
        {
            _environments = environments;
        }

        public IQueryable<Environment> GetEnvironments() =>
            _environments.AsQueryable();

        public Task<Environment> GetEnvironmentAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _environments.AsQueryable()
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task AddEnvironmentAsync(
            Environment environment,
            CancellationToken cancellationToken = default)
        {
            return _environments.InsertOneAsync(
                environment,
                options: null,
                cancellationToken);
        }

        public Task UpdateEnvironmentAsync(
            Environment environment,
            CancellationToken cancellationToken = default)
        {
            return _environments.ReplaceOneAsync(
                Builders<Environment>.Filter.Eq(t => t.Id, environment.Id),
                environment,
                options: default(ReplaceOptions),
                cancellationToken);
        }
    }
}
