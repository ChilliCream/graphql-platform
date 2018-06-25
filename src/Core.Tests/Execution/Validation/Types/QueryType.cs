using HotChocolate.Types;

namespace HotChocolate.Execution.Validation
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
        {
        }
    }
}
