using HotChocolate.Configuration;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __EnumValue
        : ObjectType<EnumValue>
    {
        protected override void Configure(IObjectTypeDescriptor<EnumValue> descriptor)
        {
            descriptor.Name("__EnumValue");

            descriptor.Description(
                "One possible value for a given Enum. Enum values are unique values, not " +
                "a placeholder for a string or numeric value. However an Enum value is " +
                "returned in a JSON response as a string.");

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field(c => c.Name)
                .Type<NonNullType<StringType>>();

            descriptor.Field(c => c.Description);

            descriptor.Field(c => c.IsDeprecated)
                .Type<NonNullType<BooleanType>>();

            descriptor.Field(c => c.DeprecationReason);
        }
    }
}
