using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class LifeInsuranceContractType
        : ObjectType<LifeInsuranceContract>
    {
        protected override void Configure(
            IObjectTypeDescriptor<LifeInsuranceContract> descriptor)
        {
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            descriptor.Field(t => t.CustomerId).Ignore();
        }
    }
}
