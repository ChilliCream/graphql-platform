using System.Linq;
using HotChocolate.Types;
using HotChocolate;
using MarshmallowPie.Repositories;
using HotChocolate.Types.Relay;

namespace MarshmallowPie.GraphQL.Schemas
{
    [ExtendObjectType(Name = "Schema")]
    public class SchemaExtension
    {
        [UsePaging(SchemaType = typeof(NonNullType<SchemaVersionType>))]
        [UseFiltering]
        [UseSorting]
        public IQueryable<SchemaVersion> GetVersions(
            [Parent]Schema schema,
            [Service]ISchemaRepository repository) =>
            repository.GetSchemaVersions().Where(t => t.SchemaId == schema.Id);
    }
}
