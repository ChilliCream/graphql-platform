using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Types
{
    public class MutationType : ObjectType
    {
        protected override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Mutation");
        }
    }
}
