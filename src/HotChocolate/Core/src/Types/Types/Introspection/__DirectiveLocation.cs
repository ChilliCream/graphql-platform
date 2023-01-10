#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;
using Lang = HotChocolate.Language.DirectiveLocation;

#nullable enable
namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __DirectiveLocation : EnumType<DirectiveLocation>
{
    protected override EnumTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        => new(
            Names.__DirectiveLocation,
            DirectiveLocation_Description,
            typeof(DirectiveLocation))
        {
            Values =
            {
                new EnumValueDefinition(
                    Lang.Query.Value,
                    DirectiveLocation_Query,
                    DirectiveLocation.Query),
                new EnumValueDefinition(
                    Lang.Mutation.Value,
                    DirectiveLocation_Mutation,
                    DirectiveLocation.Mutation),
                new EnumValueDefinition(
                    Lang.Subscription.Value,
                    DirectiveLocation_Subscription,
                    DirectiveLocation.Subscription),
                new EnumValueDefinition(
                    Lang.Field.Value,
                    DirectiveLocation_Field,
                    DirectiveLocation.Field),
                new EnumValueDefinition(
                    Lang.FragmentDefinition.Value,
                    DirectiveLocation_FragmentDefinition,
                    DirectiveLocation.FragmentDefinition),
                new EnumValueDefinition(
                    Lang.FragmentSpread.Value,
                    DirectiveLocation_FragmentSpread,
                    DirectiveLocation.FragmentSpread),
                new EnumValueDefinition(
                    Lang.InlineFragment.Value,
                    DirectiveLocation_InlineFragment,
                    DirectiveLocation.InlineFragment),
                new EnumValueDefinition(
                    Lang.VariableDefinition.Value,
                    DirectiveLocation_VariableDefinition,
                    DirectiveLocation.VariableDefinition),
                new EnumValueDefinition(
                    Lang.Schema.Value,
                    DirectiveLocation_Schema,
                    DirectiveLocation.Schema),
                new EnumValueDefinition(
                    Lang.Scalar.Value,
                    DirectiveLocation_Scalar,
                    DirectiveLocation.Scalar),
                new EnumValueDefinition(
                    Lang.Object.Value,
                    DirectiveLocation_Object,
                    DirectiveLocation.Object),
                new EnumValueDefinition(
                    Lang.FieldDefinition.Value,
                    DirectiveLocation_FieldDefinition,
                    DirectiveLocation.FieldDefinition),
                new EnumValueDefinition(
                    Lang.ArgumentDefinition.Value,
                    DirectiveLocation_ArgumentDefinition,
                    DirectiveLocation.ArgumentDefinition),
                new EnumValueDefinition(
                    Lang.Interface.Value,
                    DirectiveLocation_Interface,
                    DirectiveLocation.Interface),
                new EnumValueDefinition(
                    Lang.Union.Value,
                    DirectiveLocation_Union,
                    DirectiveLocation.Union),
                new EnumValueDefinition(
                    Lang.Enum.Value,
                    DirectiveLocation_Enum,
                    DirectiveLocation.Enum),
                new EnumValueDefinition(
                    Lang.EnumValue.Value,
                    DirectiveLocation_EnumValue,
                    DirectiveLocation.EnumValue),
                new EnumValueDefinition(
                    Lang.InputObject.Value,
                    DirectiveLocation_InputObject,
                    DirectiveLocation.InputObject),
                new EnumValueDefinition(
                    Lang.InputFieldDefinition.Value,
                    DirectiveLocation_InputFieldDefinition,
                    DirectiveLocation.InputFieldDefinition),
            }
        };

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __DirectiveLocation = "__DirectiveLocation";
    }
}
#pragma warning restore IDE1006 // Naming Styles
