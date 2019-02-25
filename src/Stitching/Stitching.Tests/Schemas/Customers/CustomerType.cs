using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class CustomerType
        : ObjectType<Customer>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Customer> descriptor)
        {
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
            descriptor.Field(t => t.ConsultantId).Ignore();

            descriptor.Field<CustomerResolver>(
                t => t.GetConsultant(default, default))
                .Type<ConsultantType>();

            descriptor.Field("say")
                .Argument("input", a => a.Type<InputObjectType<SayInput>>())
                .Type<StringType>()
                .Resolver(ctx => ctx.Argument<SayInput>("input").Word);
        }
    }
}
