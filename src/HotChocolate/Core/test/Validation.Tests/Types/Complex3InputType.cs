using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class Complex3InputType
    : InputObjectType
{
    protected override void Configure(
        IInputObjectTypeDescriptor descriptor)
    {
        descriptor.Field("requiredField")
            .Type<NonNullType<BooleanType>>();

        descriptor.Field("intField")
            .Type<IntType>();

        descriptor.Field("stringField")
            .Type<StringType>();

        descriptor.Field("booleanField")
            .Type<BooleanType>();

        descriptor.Field("stringListField")
            .Type<ListType<StringType>>();

        descriptor.Field("nonNullField")
            .Type<NonNullType<BooleanType>>()
            .DefaultValue(true);

        descriptor.Field("f")
            .Type<BooleanType>();

        descriptor.Field("f1")
            .Type<StringType>();

        descriptor.Field("f2")
            .Type<StringType>();

        descriptor.Field("f3")
            .Type<StringType>();

        descriptor.Field("id")
            .Type<IntType>();

        descriptor.Field("deep")
            .Type<Complex3InputType>();
    }
}
