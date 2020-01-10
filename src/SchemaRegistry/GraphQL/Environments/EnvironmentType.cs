using HotChocolate;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Environments
{
    public class EnvironmentType : ObjectType<Environment>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Environment> descriptor)
        {
            descriptor
                .AsNode()
                .IdField(t => t.Id)
                .NodeResolver((ctx, id) =>
                    ctx.DataLoader<EnvironmentByIdDataLoader>().LoadAsync(id, ctx.RequestAborted));
        }
    }
}
