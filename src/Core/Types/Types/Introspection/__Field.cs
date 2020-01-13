using HotChocolate.Properties;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __Field
#pragma warning restore IDE1006 // Naming Styles
        : ObjectType<IOutputField>
    {
        protected override void Configure(
            IObjectTypeDescriptor<IOutputField> descriptor)
        {
            descriptor.Name("__Field");
            descriptor.Description(TypeResources.Field_Description);

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field(t => t.Name).NonNullType(Scalars.String);

            descriptor.Field(t => t.Description).Type(Scalars.String);

            descriptor.Field(t => t.Arguments)
                .Name("args")
                .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
                .Resolver(c => c.Parent<IOutputField>().Arguments);

            descriptor.Field(t => t.Type).Type<NonNullType<__Type>>();

            descriptor.Field(t => t.IsDeprecated).NonNullType(Scalars.Boolean);

            descriptor.Field(t => t.DeprecationReason).Type(Scalars.String);
        }
    }
}
