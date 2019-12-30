using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Schemas
{
    public sealed class SchemaVersionDataLoader
        : BatchDataLoader<Guid, SchemaVersion>
    {
        private readonly ISchemaRepository _repository;

        public SchemaVersionDataLoader(ISchemaRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<Guid, SchemaVersion>> FetchBatchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken) =>
            _repository.GetSchemaVersionsAsync(keys, cancellationToken);
    }
}
