using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Schemas
{
    public sealed class SchemaByNameDataLoader
        : BatchDataLoader<string, Schema>
    {
        private readonly ISchemaRepository _repository;

        public SchemaByNameDataLoader(ISchemaRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<string, Schema>> FetchBatchAsync(
            IReadOnlyList<string> keys,
            CancellationToken cancellationToken) =>
            _repository.GetSchemasAsync(keys, cancellationToken);
    }
}
