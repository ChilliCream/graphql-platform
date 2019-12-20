using HotChocolate.Types;

namespace MarshmallowPie.GraphQL.Types
{
    public class QueryType : ObjectType
    {
        protected override void Configure(
            IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");
        }
    }
}
