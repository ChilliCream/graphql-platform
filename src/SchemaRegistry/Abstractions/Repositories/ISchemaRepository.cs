using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace MarshmallowPie.Repositories
{
    public interface ISchemaRepository
    {
        IQueryable<Schema> GetSchemas();

        Task<Schema> GetSchemaAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, Schema>> GetSchemasAsync(
            IReadOnlyList<Guid> ids,
            CancellationToken cancellationToken = default);

        Task AddSchemaAsync(
            Schema schema,
            CancellationToken cancellationToken = default);

        Task UpdateSchemaAsync(
            Schema schema,
            CancellationToken cancellationToken = default);

        IQueryable<SchemaVersion> GetSchemaVersions();

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
