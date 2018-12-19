using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class ComplexInputType
        : InputObjectType<ComplexInput>
    {
        protected override void Configure(
            IInputObjectTypeDescriptor<ComplexInput> descriptor)
        {
        }
    }
}
