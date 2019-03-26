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
            descriptor.Field("error")
                .Type<StringType>()
                .Resolver(ctx => ErrorBuilder.New()
                    .SetMessage("Error_Message")
                    .SetCode("ERROR_CODE")
                    .SetPath(ctx.Path)
                    .SetExtension("EXT_KEY", "EXT_VALUE")
                    .Build());
        }
    }
}
