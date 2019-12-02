using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace MarshmallowPie.Repositories
{
    public interface ISchemaRepository
    {
        IQueryable<Schema> Schemas { get; }

        Task<IReadOnlyDictionary<Guid, Schema>> GetSchemasAsync(
            IReadOnlyList<Guid> ids);

        Task AddSchemaAsync(Schema schema);

        Task UpdateSchemaAsync(SchemaVersion schema);

        IQueryable<SchemaVersion> SchemaVersions { get; }

        Task<IReadOnlyDictionary<Guid, SchemaVersion>> GetSchemaVersionsAsync(
            IReadOnlyList<Guid> ids);

        Task AddSchemaVersionAsync(SchemaVersion schema);

        Task UpdateSchemaVersionAsync(SchemaVersion schema);
    }
}
