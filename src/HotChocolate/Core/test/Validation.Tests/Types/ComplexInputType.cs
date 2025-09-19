using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class ComplexInputType
    : InputObjectType<ComplexInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<ComplexInput> descriptor)
    {
    }
}
