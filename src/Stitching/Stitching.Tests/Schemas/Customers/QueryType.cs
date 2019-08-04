using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class QueryType
        : ObjectType<Query>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Query> descriptor)
        {
            descriptor.Field(t => t.GetCustomer(default(string)))
                .Argument("id", a => a.Type<NonNullType<IdType>>())
                .Type<CustomerType>();

            descriptor.Field(t => t.GetCustomers(default(string[])))
                .Argument("ids", a => a.Type<NonNullType<ListType<NonNullType<IdType>>>>())
                .Type<ListType<CustomerType>>();

            descriptor.Field(t => t.GetConsultant(default))
                .Argument("id", a => a.Type<NonNullType<IdType>>())
                .Type<ConsultantType>();

            descriptor.Field(t => t.GetCustomerOrConsultant(default))
                .Argument("id", a => a.Type<NonNullType<IdType>>())
                .Type<CustomerOrConsultantType>();

            descriptor.Field(t => t.GetCustomer(default(CustomerKind)))
                .Name("customerByKind")
                .Type<CustomerType>();
        }
    }
}
