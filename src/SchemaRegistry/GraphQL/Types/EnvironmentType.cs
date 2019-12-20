using HotChocolate;
using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Types
{
    public class EnvironmentType : ObjectType<Environment>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Environment> descriptor)
        {
            descriptor
                .AsNode()
                .IdField(t => t.Id);
        }
    }
}
