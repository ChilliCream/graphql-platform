using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class ConsultantType
        : ObjectType<Consultant>
    {
        protected override void Configure(
            IObjectTypeDescriptor<Consultant> descriptor)
        {
            descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
        }
    }
}
