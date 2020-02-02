using System;
using System.Collections.Generic;
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
            _environments.Indexes.CreateOne(
                new CreateIndexModel<Environment>(
                    Builders<Environment>.IndexKeys.Ascending(x => x.Name),
                    new CreateIndexOptions { Unique = true }));
        }

        public IQueryable<Environment> GetEnvironments() =>
            _environments.AsQueryable();

        public Task<Environment> GetEnvironmentAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return _environments.AsQueryable()
                .Where(t => t.Id == id)
                .FirstAsync(cancellationToken);
        }

        public Task<Environment?> GetEnvironmentAsync(
            string name,
            CancellationToken cancellationToken = default)
        {
            return _environments.AsQueryable()
                .Where(t => t.Name == name)
                .FirstOrDefaultAsync(cancellationToken)!;
        }

        public async Task<IReadOnlyDictionary<Guid, Environment>> GetEnvironmentsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            var list = new List<Guid>(ids);

            List<Environment> result = await _environments.AsQueryable()
                .Where(t => list.Contains(t.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Id);
        }

        public async Task<IReadOnlyDictionary<string, Environment>> GetEnvironmentsAsync(
            IReadOnlyList<string> names,
            CancellationToken cancellationToken = default)
        {
            var list = new List<string>(names);

            List<Environment> result = await _environments.AsQueryable()
                .Where(t => list.Contains(t.Name))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.ToDictionary(t => t.Name);
        }

        public async Task AddEnvironmentAsync(
            Environment environment,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _environments.InsertOneAsync(
                    environment,
                    options: null,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"An environment with the name `{environment.Name}` already exists",
                    ex);
            }
        }

        public async Task UpdateEnvironmentAsync(
            Environment environment,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _environments.ReplaceOneAsync(
                    Builders<Environment>.Filter.Eq(t => t.Id, environment.Id),
                    environment,
                    options: default(ReplaceOptions),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // TODO : resources
                throw new DuplicateKeyException(
                    $"An environment with the name `{environment.Name}` already exists",
                    ex);
            }
        }
    }
}
