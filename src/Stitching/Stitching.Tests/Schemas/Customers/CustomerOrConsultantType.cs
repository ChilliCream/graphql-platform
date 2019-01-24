using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class CustomerOrConsultantType
        : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name("CustomerOrConsultant");
            descriptor.Type<CustomerType>();
            descriptor.Type<ConsultantType>();
        }
    }
}
