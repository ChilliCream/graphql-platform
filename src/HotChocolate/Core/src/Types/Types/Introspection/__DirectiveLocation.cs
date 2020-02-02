using HotChocolate.Properties;

namespace HotChocolate.Types.Introspection
{
    [Introspection]
#pragma warning disable IDE1006 // Naming Styles
    internal sealed class __DirectiveLocation
#pragma warning restore IDE1006 // Naming Styles
        : EnumType<DirectiveLocation>
    {
        protected override void Configure(IEnumTypeDescriptor<DirectiveLocation> descriptor)
        {
            descriptor.Name("__DirectiveLocation");

            descriptor.Description(
                TypeResources.DirectiveLocation_Description);

            descriptor.Item(DirectiveLocation.Query)
                .Description(TypeResources.DirectiveLocation_Query);

            descriptor.Item(DirectiveLocation.Mutation)
                .Description(TypeResources.DirectiveLocation_Mutation);

            descriptor.Item(DirectiveLocation.Subscription)
                .Description(TypeResources.DirectiveLocation_Subscription);

            descriptor.Item(DirectiveLocation.Field)
                .Description(TypeResources.DirectiveLocation_Field);

            descriptor.Item(DirectiveLocation.FragmentDefinition)
                .Name("FRAGMENT_DEFINITION")
                .Description(TypeResources.DirectiveLocation_FragmentDefinition);

            descriptor.Item(DirectiveLocation.FragmentSpread)
                .Name("FRAGMENT_SPREAD")
                .Description(TypeResources.DirectiveLocation_FragmentSpread);

            descriptor.Item(DirectiveLocation.InlineFragment)
                .Name("INLINE_FRAGMENT")
                .Description(TypeResources.DirectiveLocation_InlineFragment);

            descriptor.Item(DirectiveLocation.Schema)
                .Description(TypeResources.DirectiveLocation_Schema);

            descriptor.Item(DirectiveLocation.Scalar)
                .Description(TypeResources.DirectiveLocation_Scalar);

            descriptor.Item(DirectiveLocation.Object)
                .Description(TypeResources.DirectiveLocation_Object);

            descriptor.Item(DirectiveLocation.FieldDefinition)
                .Name("FIELD_DEFINITION")
                .Description(TypeResources.DirectiveLocation_FieldDefinition);

            descriptor.Item(DirectiveLocation.ArgumentDefinition)
                .Name("ARGUMENT_DEFINITION")
                .Description(TypeResources.DirectiveLocation_ArgumentDefinition);

            descriptor.Item(DirectiveLocation.Interface)
                .Description(TypeResources.DirectiveLocation_Interface);

            descriptor.Item(DirectiveLocation.Union)
                .Description(TypeResources.DirectiveLocation_Union);

            descriptor.Item(DirectiveLocation.Enum)
                .Description(TypeResources.DirectiveLocation_Enum);

            descriptor.Item(DirectiveLocation.EnumValue)
                .Name("ENUM_VALUE")
                .Description(TypeResources.DirectiveLocation_EnumValue);

            descriptor.Item(DirectiveLocation.InputObject)
                .Name("INPUT_OBJECT")
                .Description(TypeResources.DirectiveLocation_InputObject);

            descriptor.Item(DirectiveLocation.InputFieldDefinition)
                .Name("INPUT_FIELD_DEFINITION")
                .Description(TypeResources.DirectiveLocation_InputFieldDefinition);
        }
    }
}
