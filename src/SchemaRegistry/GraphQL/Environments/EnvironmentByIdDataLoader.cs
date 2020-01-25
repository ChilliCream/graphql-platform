using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Environments
{
    public sealed class EnvironmentByIdDataLoader
        : BatchDataLoader<Guid, Environment>
    {
        private readonly IEnvironmentRepository _repository;

        public EnvironmentByIdDataLoader(IEnvironmentRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<Guid, Environment>> FetchBatchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken) =>
            _repository.GetEnvironmentsAsync(keys, cancellationToken);
    }
}
