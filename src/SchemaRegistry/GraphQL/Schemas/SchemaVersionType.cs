using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Schemas
{
    public class SchemaVersionType : ObjectType<SchemaVersion>
    {
        protected override void Configure(IObjectTypeDescriptor<SchemaVersion> descriptor)
        {
            descriptor.AsNode()
                .IdField(t => t.Id)
                .NodeResolver((context, id) =>
                context.DataLoader<SchemaVersionDataLoader>()
                    .LoadAsync(id, context.RequestAborted));

            descriptor.Ignore(t => t.SchemaId);
        }
    }
}
