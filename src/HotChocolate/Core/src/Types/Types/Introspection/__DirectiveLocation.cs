#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Properties;
using Lang = HotChocolate.Language.DirectiveLocation;

#nullable enable
namespace HotChocolate.Types.Introspection
{
    [Introspection]
    internal sealed class __DirectiveLocation : EnumType<DirectiveLocation>
    {
        protected override void Configure(IEnumTypeDescriptor<DirectiveLocation> descriptor)
        {
            descriptor
                .Name(Names.__DirectiveLocation)
                .Description(TypeResources.DirectiveLocation_Description)
                // Introspection types must always be bound explicitly so that we
                // do not get any interference with conventions.
                .BindItems(BindingBehavior.Explicit);

            descriptor
                .Item(DirectiveLocation.Query)
                .Name(Lang.Query.Value)
                .Description(TypeResources.DirectiveLocation_Query);

            descriptor
                .Item(DirectiveLocation.Mutation)
                .Name(Lang.Mutation.Value)
                .Description(TypeResources.DirectiveLocation_Mutation);

            descriptor
                .Item(DirectiveLocation.Subscription)
                .Name(Lang.Subscription.Value)
                .Description(TypeResources.DirectiveLocation_Subscription);

            descriptor
                .Item(DirectiveLocation.Field)
                .Name(Lang.Field.Value)
                .Description(TypeResources.DirectiveLocation_Field);

            descriptor
                .Item(DirectiveLocation.FragmentDefinition)
                .Name(Lang.FragmentDefinition.Value)
                .Description(TypeResources.DirectiveLocation_FragmentDefinition);

            descriptor
                .Item(DirectiveLocation.FragmentSpread)
                .Name(Lang.FragmentSpread.Value)
                .Description(TypeResources.DirectiveLocation_FragmentSpread);

            descriptor
                .Item(DirectiveLocation.InlineFragment)
                .Name(Lang.InlineFragment.Value)
                .Description(TypeResources.DirectiveLocation_InlineFragment);

            descriptor
                .Item(DirectiveLocation.VariableDefinition)
                .Name(Lang.VariableDefinition.Value)
                .Description("Location adjacent to a variable definition.");

            descriptor
                .Item(DirectiveLocation.Schema)
                .Name(Lang.Schema.Value)
                .Description(TypeResources.DirectiveLocation_Schema);

            descriptor
                .Item(DirectiveLocation.Scalar)
                .Name(Lang.Scalar.Value)
                .Description(TypeResources.DirectiveLocation_Scalar);

            descriptor
                .Item(DirectiveLocation.Object)
                .Name(Lang.Object.Value)
                .Description(TypeResources.DirectiveLocation_Object);

            descriptor
                .Item(DirectiveLocation.FieldDefinition)
                .Name(Lang.FieldDefinition.Value)
                .Description(TypeResources.DirectiveLocation_FieldDefinition);

            descriptor
                .Item(DirectiveLocation.ArgumentDefinition)
                .Name(Lang.ArgumentDefinition.Value)
                .Description(TypeResources.DirectiveLocation_ArgumentDefinition);

            descriptor
                .Item(DirectiveLocation.Interface)
                .Name(Lang.Interface.Value)
                .Description(TypeResources.DirectiveLocation_Interface);

            descriptor
                .Item(DirectiveLocation.Union)
                .Name(Lang.Union.Value)
                .Description(TypeResources.DirectiveLocation_Union);

            descriptor
                .Item(DirectiveLocation.Enum)
                .Name(Lang.Enum.Value)
                .Description(TypeResources.DirectiveLocation_Enum);

            descriptor
                .Item(DirectiveLocation.EnumValue)
                .Name(Lang.EnumValue.Value)
                .Description(TypeResources.DirectiveLocation_EnumValue);

            descriptor
                .Item(DirectiveLocation.InputObject)
                .Name(Lang.InputObject.Value)
                .Description(TypeResources.DirectiveLocation_InputObject);

            descriptor
                .Item(DirectiveLocation.InputFieldDefinition)
                .Name(Lang.InputFieldDefinition.Value)
                .Description(TypeResources.DirectiveLocation_InputFieldDefinition);
        }

        public static class Names
        {
            public const string __DirectiveLocation = "__DirectiveLocation";
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
