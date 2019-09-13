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

            descriptor.Field(t => t.Name)
                .Type<NonNullType<StringType>>();

            descriptor.Field(t => t.Description);

            descriptor.Field(t => t.Arguments)
                .Name("args")
                .Type<NonNullType<ListType<NonNullType<__InputValue>>>>()
                .Resolver(c => c.Parent<IOutputField>().Arguments);

            descriptor.Field(t => t.Type)
                .Type<NonNullType<__Type>>();

            descriptor.Field(t => t.IsDeprecated)
                .Type<NonNullType<BooleanType>>();

            descriptor.Field(t => t.DeprecationReason);
        }
    }
}
