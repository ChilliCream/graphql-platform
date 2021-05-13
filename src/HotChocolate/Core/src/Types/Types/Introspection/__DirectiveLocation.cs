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
                .BindValues(BindingBehavior.Explicit);

            descriptor
                .Value(DirectiveLocation.Query)
                .Name(Lang.Query.Value)
                .Description(TypeResources.DirectiveLocation_Query);

            descriptor
                .Value(DirectiveLocation.Mutation)
                .Name(Lang.Mutation.Value)
                .Description(TypeResources.DirectiveLocation_Mutation);

            descriptor
                .Value(DirectiveLocation.Subscription)
                .Name(Lang.Subscription.Value)
                .Description(TypeResources.DirectiveLocation_Subscription);

            descriptor
                .Value(DirectiveLocation.Field)
                .Name(Lang.Field.Value)
                .Description(TypeResources.DirectiveLocation_Field);

            descriptor
                .Value(DirectiveLocation.FragmentDefinition)
                .Name(Lang.FragmentDefinition.Value)
                .Description(TypeResources.DirectiveLocation_FragmentDefinition);

            descriptor
                .Value(DirectiveLocation.FragmentSpread)
                .Name(Lang.FragmentSpread.Value)
                .Description(TypeResources.DirectiveLocation_FragmentSpread);

            descriptor
                .Value(DirectiveLocation.InlineFragment)
                .Name(Lang.InlineFragment.Value)
                .Description(TypeResources.DirectiveLocation_InlineFragment);

            descriptor
                .Value(DirectiveLocation.VariableDefinition)
                .Name(Lang.VariableDefinition.Value)
                .Description("Location adjacent to a variable definition.");

            descriptor
                .Value(DirectiveLocation.Schema)
                .Name(Lang.Schema.Value)
                .Description(TypeResources.DirectiveLocation_Schema);

            descriptor
                .Value(DirectiveLocation.Scalar)
                .Name(Lang.Scalar.Value)
                .Description(TypeResources.DirectiveLocation_Scalar);

            descriptor
                .Value(DirectiveLocation.Object)
                .Name(Lang.Object.Value)
                .Description(TypeResources.DirectiveLocation_Object);

            descriptor
                .Value(DirectiveLocation.FieldDefinition)
                .Name(Lang.FieldDefinition.Value)
                .Description(TypeResources.DirectiveLocation_FieldDefinition);

            descriptor
                .Value(DirectiveLocation.ArgumentDefinition)
                .Name(Lang.ArgumentDefinition.Value)
                .Description(TypeResources.DirectiveLocation_ArgumentDefinition);

            descriptor
                .Value(DirectiveLocation.Interface)
                .Name(Lang.Interface.Value)
                .Description(TypeResources.DirectiveLocation_Interface);

            descriptor
                .Value(DirectiveLocation.Union)
                .Name(Lang.Union.Value)
                .Description(TypeResources.DirectiveLocation_Union);

            descriptor
                .Value(DirectiveLocation.Enum)
                .Name(Lang.Enum.Value)
                .Description(TypeResources.DirectiveLocation_Enum);

            descriptor
                .Value(DirectiveLocation.EnumValue)
                .Name(Lang.EnumValue.Value)
                .Description(TypeResources.DirectiveLocation_EnumValue);

            descriptor
                .Value(DirectiveLocation.InputObject)
                .Name(Lang.InputObject.Value)
                .Description(TypeResources.DirectiveLocation_InputObject);

            descriptor
                .Value(DirectiveLocation.InputFieldDefinition)
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
