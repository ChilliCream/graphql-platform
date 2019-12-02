using System;
using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using MarshmallowPie.GraphQL.DataLoader;

namespace MarshmallowPie.GraphQL.Types
{
    public class SchemaType : ObjectType<Schema>
    {
        protected override void Configure(IObjectTypeDescriptor<Schema> descriptor)
        {
            descriptor.AsNode()
                .IdField(t => t.Id)
                .NodeResolver((context, id) =>
                {
                    SchemaDataLoader dataLoader = context.DataLoader<SchemaDataLoader>();
                    return dataLoader.LoadAsync(id, context.RequestAborted)!;
                });
        }
    }
}
