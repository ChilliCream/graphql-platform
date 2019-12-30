using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Schemas
{
    public class SchemaDataLoader
        : DataLoaderBase<Guid, Schema?>
    {
        private readonly ISchemaRepository _repository;

        public SchemaDataLoader(ISchemaRepository repository)
        {
            _repository = repository;
        }

        protected override async Task<IReadOnlyList<Result<Schema?>>> FetchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, Schema> schemas =
                await _repository.GetSchemasAsync(keys, cancellationToken);

            var list = new List<Result<Schema?>>();
            for (int i = 0; i < keys.Count; i++)
            {
                list[i] = schemas.TryGetValue(keys[i], out Schema? schema)
                    ? Result<Schema?>.Resolve(schema)
                    : Result<Schema?>.Resolve(null);
            }
            return list;
        }
    }
}
