using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace MarshmallowPie.Repositories
{
    public interface ISchemaRepository
    {
        IQueryable<Schema> Schemas { get; }

        IQueryable<SchemaVersion> SchemaVersions { get; }

        Task<IReadOnlyDictionary<Guid, Schema>> GetSchemasAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken);

        Task AddSchemaAsync(
            Schema schema,
            CancellationToken cancellationToken);

        Task UpdateSchemaAsync(
            SchemaVersion schema,
            CancellationToken cancellationToken);

        Task<IReadOnlyDictionary<Guid, SchemaVersion>> GetSchemaVersionsAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken);

        Task AddSchemaVersionAsync(
            SchemaVersion schema,
            CancellationToken cancellationToken);

        Task UpdateSchemaVersionAsync(
            SchemaVersion schema,
            CancellationToken cancellationToken);
    }
}
