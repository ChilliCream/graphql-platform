#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Properties.TypeResources;
using Lang = HotChocolate.Language.DirectiveLocation;

#nullable enable
namespace HotChocolate.Types.Introspection;

[Introspection]
// ReSharper disable once InconsistentNaming
internal sealed class __DirectiveLocation : EnumType<DirectiveLocation>
{
    protected override EnumTypeConfiguration CreateConfiguration(ITypeDiscoveryContext context)
        => new(
            Names.__DirectiveLocation,
            DirectiveLocation_Description,
            typeof(DirectiveLocation))
        {
            Values =
            {
                new EnumValueConfiguration(
                    Lang.Query.Value,
                    DirectiveLocation_Query,
                    DirectiveLocation.Query),
                new EnumValueConfiguration(
                    Lang.Mutation.Value,
                    DirectiveLocation_Mutation,
                    DirectiveLocation.Mutation),
                new EnumValueConfiguration(
                    Lang.Subscription.Value,
                    DirectiveLocation_Subscription,
                    DirectiveLocation.Subscription),
                new EnumValueConfiguration(
                    Lang.Field.Value,
                    DirectiveLocation_Field,
                    DirectiveLocation.Field),
                new EnumValueConfiguration(
                    Lang.FragmentDefinition.Value,
                    DirectiveLocation_FragmentDefinition,
                    DirectiveLocation.FragmentDefinition),
                new EnumValueConfiguration(
                    Lang.FragmentSpread.Value,
                    DirectiveLocation_FragmentSpread,
                    DirectiveLocation.FragmentSpread),
                new EnumValueConfiguration(
                    Lang.InlineFragment.Value,
                    DirectiveLocation_InlineFragment,
                    DirectiveLocation.InlineFragment),
                new EnumValueConfiguration(
                    Lang.VariableDefinition.Value,
                    DirectiveLocation_VariableDefinition,
                    DirectiveLocation.VariableDefinition),
                new EnumValueConfiguration(
                    Lang.Schema.Value,
                    DirectiveLocation_Schema,
                    DirectiveLocation.Schema),
                new EnumValueConfiguration(
                    Lang.Scalar.Value,
                    DirectiveLocation_Scalar,
                    DirectiveLocation.Scalar),
                new EnumValueConfiguration(
                    Lang.Object.Value,
                    DirectiveLocation_Object,
                    DirectiveLocation.Object),
                new EnumValueConfiguration(
                    Lang.FieldDefinition.Value,
                    DirectiveLocation_FieldDefinition,
                    DirectiveLocation.FieldDefinition),
                new EnumValueConfiguration(
                    Lang.ArgumentDefinition.Value,
                    DirectiveLocation_ArgumentDefinition,
                    DirectiveLocation.ArgumentDefinition),
                new EnumValueConfiguration(
                    Lang.Interface.Value,
                    DirectiveLocation_Interface,
                    DirectiveLocation.Interface),
                new EnumValueConfiguration(
                    Lang.Union.Value,
                    DirectiveLocation_Union,
                    DirectiveLocation.Union),
                new EnumValueConfiguration(
                    Lang.Enum.Value,
                    DirectiveLocation_Enum,
                    DirectiveLocation.Enum),
                new EnumValueConfiguration(
                    Lang.EnumValue.Value,
                    DirectiveLocation_EnumValue,
                    DirectiveLocation.EnumValue),
                new EnumValueConfiguration(
                    Lang.InputObject.Value,
                    DirectiveLocation_InputObject,
                    DirectiveLocation.InputObject),
                new EnumValueConfiguration(
                    Lang.InputFieldDefinition.Value,
                    DirectiveLocation_InputFieldDefinition,
                    DirectiveLocation.InputFieldDefinition)
            }
        };

    public static class Names
    {
        // ReSharper disable once InconsistentNaming
        public const string __DirectiveLocation = "__DirectiveLocation";
    }
}
#pragma warning restore IDE1006 // Naming Styles
