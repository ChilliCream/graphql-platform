using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class CustomerType
        : ObjectType<Customer>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Customer> descriptor)
        {
            descriptor
                .AsNode()
                .IdField(t => t.Id)
                .NodeResolver((ctx, id) => 
                {
                    return Task.FromResult(
                        ctx.Service<CustomerRepository>()
                            .Customers.FirstOrDefault(t => t.Id.Equals(id)));
                });

            descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
            descriptor.Field(t => t.Street).Type<NonNullType<StringType>>();
            descriptor.Field(t => t.ConsultantId).Ignore();

            descriptor.Field<CustomerResolver>(
                t => t.GetConsultant(default, default))
                .Type<ConsultantType>();

            descriptor.Field("say")
                .Argument("input", a =>
                    a.Type<NonNullType<InputObjectType<SayInput>>>())
                .Type<StringType>()
                .Resolver(ctx => string.Join(", ",
                    ctx.ArgumentValue<SayInput>("input").Words));

            descriptor.Field("complexArg")
                .Argument("arg", a =>
                    a.Type<ComplexInputType>())
                .Type<StringType>()
                .Resolver("");
        }
    }
}
