using HotChocolate.Properties;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __EnumValue
#pragma warning restore IDE1006 // Naming Styles
        : ObjectType<EnumValue>
    {
        protected override void Configure(
            IObjectTypeDescriptor<EnumValue> descriptor)
        {
            descriptor.Name("__EnumValue");
            descriptor.Description(TypeResources.EnumValue_Description);

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field(c => c.Name).NonNullType(Scalars.String);
            descriptor.Field(c => c.Description).Type(Scalars.String);
            descriptor.Field(c => c.IsDeprecated).NonNullType(Scalars.Boolean);
            descriptor.Field(c => c.DeprecationReason).Type(Scalars.String);
        }
    }
}
