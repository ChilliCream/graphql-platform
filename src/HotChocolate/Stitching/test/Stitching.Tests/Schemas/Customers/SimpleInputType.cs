using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class SimpleInputType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Name("SimpleInputType");
            descriptor.Field("value").Type<StringType>();
        }
    }
}
