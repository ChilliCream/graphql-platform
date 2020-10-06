using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class SomeOtherContractType
        : ObjectType<SomeOtherContract>
    {
        protected override void Configure(
            IObjectTypeDescriptor<SomeOtherContract> descriptor)
        {
            descriptor.Interface<ContractType>();

            descriptor.Field(t => t.Id)
                .Type<NonNullType<IdType>>();

            descriptor.Field(t => t.CustomerId)
                .Type<NonNullType<IdType>>();

            descriptor.Field(t => t.ExpiryDate)
                .Type<NonNullType<DateTimeType>>();
        }
    }
}
