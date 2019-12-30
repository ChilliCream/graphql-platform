using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL
{
    public sealed class EnvironmentDataLoader
        : BatchDataLoader<Guid, Environment>
    {
        private readonly IEnvironmentRepository _repository;

        public EnvironmentDataLoader(IEnvironmentRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<Guid, Environment>> FetchBatchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken) =>
            _repository.GetEnvironmentsAsync(keys, cancellationToken);
    }
}
