using HotChocolate.Data.Filters;
using HotChocolate.Types.MongoDb;

public class ObjectIdOperationFilterInputType
    : ComparableOperationFilterInputType<ObjectIdType>
{
    protected override void Configure(IFilterInputTypeDescriptor descriptor)
    {
        descriptor.Name("ObjectIdOperationFilterInput");
        base.Configure(descriptor);
    }
}
