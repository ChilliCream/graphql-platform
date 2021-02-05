using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Data.Filters
{
    public class IdOperationFilterInputType<TEntityType, TIdType>
        : FilterInputType
        , IComparableOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotEquals)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.In)
                .Type<ListType<IdType>>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotIn)
                .Type<ListType<IdType>>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.GreaterThan)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThan)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.GreaterThanOrEquals)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotGreaterThanOrEquals)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.LowerThan)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotLowerThan)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.LowerThanOrEquals)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.Operation(DefaultFilterOperations.NotLowerThanOrEquals)
                .Type<IdType>()
                .ID<TEntityType, TIdType>()
                .MakeNullable();
            descriptor.AllowAnd(false).AllowOr(false);
        }

        protected override void OnRegisterDependencies(
            ITypeDiscoveryContext context,
            InputObjectTypeDefinition definition)
        {
            IExtendedType ouputType =
                context.TypeInspector.GetType(typeof(ObjectType<TEntityType>));
            context.RegisterDependency(TypeDependency.FromSchemaType(ouputType));
            base.OnRegisterDependencies(context, definition);
        }
    }
}
