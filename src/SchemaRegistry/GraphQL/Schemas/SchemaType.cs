using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Schemas
{
    public class SchemaType : ObjectType<Schema>
    {
        protected override void Configure(IObjectTypeDescriptor<Schema> descriptor)
        {
            descriptor.AsNode()
                .IdField(t => t.Id)
                .NodeResolver((context, id) =>
                context.DataLoader<SchemaDataLoader>().LoadAsync(id, context.RequestAborted));
        }
    }
}
