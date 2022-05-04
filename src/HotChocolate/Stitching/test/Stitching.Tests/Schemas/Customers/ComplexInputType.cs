using HotChocolate.Types;

namespace HotChocolate.Stitching.Schemas.Customers;

public class ComplexInputType : InputObjectType<ComplexInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<ComplexInput> descriptor)
    {
        descriptor.Name("ComplexInputType");
        descriptor.Field(t => t.Value).Type<StringType>();
        descriptor.Field(t => t.Deeper).Type<ComplexInputType>();
    }
}
