using HotChocolate.Types;

namespace HotChocolate.Validation.Types
{
    public class ComplexInputUnionNestedType : InputObjectType
    {
        protected override void Configure(IInputObjectTypeDescriptor descriptor)
        {
            descriptor.Field("nested").Type<ComplexInputUnionType>();
            descriptor.Field("nestedNonNull").Type<NonNullType<ComplexInputUnionType>>();
        }
    }
}
