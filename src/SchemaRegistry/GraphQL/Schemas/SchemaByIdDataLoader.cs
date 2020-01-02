using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Schemas
{
    public sealed class SchemaByIdDataLoader
        : BatchDataLoader<Guid, Schema>
    {
        private readonly ISchemaRepository _repository;

        public SchemaByIdDataLoader(ISchemaRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<Guid, Schema>> FetchBatchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken) =>
            _repository.GetSchemasAsync(keys, cancellationToken);
    }
}
