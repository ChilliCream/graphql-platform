using HotChocolate.Types;

namespace HotChocolate.Validation.Types
{
    public class ComplexInputUnionType : InputUnionType
    {
        protected override void Configure(IInputUnionTypeDescriptor descriptor)
        {
            descriptor.Type<ComplexInputType>();
            descriptor.Type<ComplexInput3Type>();
        }
    }
}
