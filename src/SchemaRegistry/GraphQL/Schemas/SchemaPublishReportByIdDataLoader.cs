using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarshmallowPie.Repositories;

namespace MarshmallowPie.GraphQL.Schemas
{
    public sealed class SchemaPublishReportByIdDataLoader
        : BatchDataLoader<Guid, SchemaPublishReport>
    {
        private readonly ISchemaRepository _repository;

        public SchemaPublishReportByIdDataLoader(ISchemaRepository repository)
        {
            _repository = repository;
        }

        protected override Task<IReadOnlyDictionary<Guid, SchemaPublishReport>> FetchBatchAsync(
            IReadOnlyList<Guid> keys,
            CancellationToken cancellationToken) =>
            _repository.GetPublishReportsAsync(keys, cancellationToken);
    }
}
