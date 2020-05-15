using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers
{
    public class ComplexOrSimpleInputType
        : InputUnionType
    {
        protected override void Configure(IInputUnionTypeDescriptor descriptor)
        {
            descriptor.Name("ComplexOrSimpleInput");
            descriptor.Type<ComplexInputType>();
            descriptor.Type<SimpleInputType>();
        }
    }
}
