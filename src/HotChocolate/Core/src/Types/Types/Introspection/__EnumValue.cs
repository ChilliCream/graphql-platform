using HotChocolate.Properties;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __EnumValue
#pragma warning restore IDE1006 // Naming Styles
        : ObjectType<IEnumValue>
    {
        protected override void Configure(
            IObjectTypeDescriptor<IEnumValue> descriptor)
        {
            descriptor.Name("__EnumValue");

            descriptor.Description(
                TypeResources.EnumValue_Description);

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
