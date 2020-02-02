using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Environments
{
    public sealed class EnvironmentByNameDataLoader
        : BatchDataLoader<string, Environment>
    {
        private readonly IEnvironmentRepository _repository;

        public EnvironmentByNameDataLoader(IEnvironmentRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<string, Environment>> FetchBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken) =>
            _repository.GetEnvironmentsAsync(keys, cancellationToken);
    }
}
