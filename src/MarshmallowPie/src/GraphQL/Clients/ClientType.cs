using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Clients
{
    public class ClientType : ObjectType<Client>
    {
        protected override void Configure(IObjectTypeDescriptor<Client> descriptor)
        {
            descriptor.AsNode()
                .IdField(t => t.Id)
                .NodeResolver((context, id) =>
                context.DataLoader<ClientByIdDataLoader>().LoadAsync(id, context.RequestAborted));
        }
    }
}
