using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Contracts
{
    public class LifeInsuranceContractType
        : ObjectType<LifeInsuranceContract>
    {
        protected override void Configure(
            IObjectTypeDescriptor<LifeInsuranceContract> descriptor)
        {
            descriptor.Interface<ContractType>();
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            descriptor.Field(t => t.CustomerId).Type<NonNullType<IdType>>();
            descriptor.Field("foo")
                .Argument("bar", a => a.Type<StringType>())
                .Resolver(ctx => ctx.Argument<string>("bar"));
        }
    }
}
