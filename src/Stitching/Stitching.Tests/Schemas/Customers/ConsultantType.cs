using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class ConsultantType
        : ObjectType<Consultant>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Consultant> descriptor)
        {
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
            descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
            descriptor.Field("customers")
                .UsePaging<CustomerType>()
                .Resolver(ctx =>
                {
                    Consultant consultant = ctx.Parent<Consultant>();
                    return ctx.Service<CustomerRepository>().Customers
                        .Where(t => t.ConsultantId == consultant.Id);
                });
        }
    }
}
