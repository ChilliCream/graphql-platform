using HotChocolate.Data.Filters;
using HotChocolate.Types;
using HotChocolate.Types.MongoDb;

namespace HotChocolate.Data.MongoDb.Filters
{
    public class ObjectIdOperationFilterInputType
        : FilterInputType
        , IComparableOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor
                .Operation(DefaultFilterOperations.Equals)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.NotEquals)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.In)
                .Type<ListType<ObjectIdType>>();

            descriptor
                .Operation(DefaultFilterOperations.NotIn)
                .Type<ListType<ObjectIdType>>();

            descriptor
                .Operation(DefaultFilterOperations.GreaterThan)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.NotGreaterThan)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.GreaterThanOrEquals)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.NotGreaterThanOrEquals)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.LowerThan)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.NotLowerThan)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.LowerThanOrEquals)
                .Type<ObjectIdType>();

            descriptor
                .Operation(DefaultFilterOperations.NotLowerThanOrEquals)
                .Type<ObjectIdType>();

            descriptor
                .AllowAnd(false)
                .AllowOr(false);
        }
    }
}
