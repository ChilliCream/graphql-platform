using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class Complex2InputType
    : InputObjectType<ComplexInput2>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<ComplexInput2> descriptor)
    {
        descriptor.Field(t => t.Name)
            .Type<NonNullType<StringType>>();
        descriptor.Field(t => t.Owner)
            .Type<NonNullType<StringType>>()
            .DefaultValue(new StringValueNode("1234"));
    }
}
