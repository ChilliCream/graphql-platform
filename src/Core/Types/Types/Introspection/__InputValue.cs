using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __InputValue
#pragma warning restore IDE1006 // Naming Styles
        : ObjectType<IInputField>
    {
        protected override void Configure(
            IObjectTypeDescriptor<IInputField> descriptor)
        {
            descriptor.Name("__InputValue");
            descriptor.Description(TypeResources.InputValue_Description);

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field(t => t.Name).NonNullType(Scalars.String);

            descriptor.Field(t => t.Description).Type(Scalars.String);

            descriptor.Field(t => t.Type).Type<NonNullType<__Type>>();

            descriptor.Field(t => t.DefaultValue)
                .Description(TypeResources.InputValue_DefaultValue)
                .Type(Scalars.String)
                .Resolver(c =>
                {
                    IInputField field = c.Parent<IInputField>();

                    if (field.DefaultValue.IsNull())
                    {
                        return null;
                    }

                    if (field.DefaultValue is { })
                    {
                        return QuerySyntaxSerializer.Serialize(field.DefaultValue);
                    }

                    return null;
                });
        }
    }
}
