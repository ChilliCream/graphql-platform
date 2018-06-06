using HotChocolate.Configuration;

namespace HotChocolate.Types.Introspection
{
    internal sealed class __InputValue
        : ObjectType<InputField>
    {
        protected override void Configure(IObjectTypeDescriptor<InputField> descriptor)
        {
            descriptor.Name("__InputValue");

            descriptor.Description(
                "Arguments provided to Fields or Directives and the input fields of an " +
                "InputObject are represented as Input Values which describe their type " +
                "and optionally a default value.");

            descriptor.BindFields(BindingBehavior.Explicit);

            descriptor.Field(t => t.Name)
                .Type<NonNullType<StringType>>();

            descriptor.Field(t => t.Description);

            descriptor.Field(t => t.Type)
                .Type<NonNullType<__Type>>();

            descriptor.Field(t => t.DefaultValue)
                .Description(
                    "A GraphQL-formatted string representing the default value for this " +
                    "input value.")
                .Type<StringType>()
                .Resolver(c =>
                {
                    InputField field = c.Parent<InputField>();
                    object obj = field.Type.ParseLiteral(field.DefaultValue);
                    return obj.ToString(); // TODO : fix this... check what the reference impl does.
                });
        }
    }
}
