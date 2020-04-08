using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class ComplexInput3Type
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
        }
    }
}
