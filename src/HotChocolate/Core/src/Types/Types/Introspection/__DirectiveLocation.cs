#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Properties.TypeResources;
using Lang = HotChocolate.Language.DirectiveLocation;

#nullable enable
namespace HotChocolate.Types.Introspection;

[Introspection]
internal sealed class __DirectiveLocation : EnumType<DirectiveLocation>
{
    protected override EnumTypeDefinition CreateDefinition(ITypeDiscoveryContext context)
        => new(Names.__DirectiveLocation,
            DirectiveLocation_Description,
            typeof(DirectiveLocation))
        {
            Values =
            {
                    new(Lang.Query.Value,
                        DirectiveLocation_Query,
                        DirectiveLocation.Query),
                    new(Lang.Mutation.Value,
                        DirectiveLocation_Mutation,
                        DirectiveLocation.Mutation),
                    new(Lang.Subscription.Value,
                        DirectiveLocation_Subscription,
                        DirectiveLocation.Subscription),
                    new(Lang.Field.Value,
                        DirectiveLocation_Field,
                        DirectiveLocation.Field),
                    new(Lang.FragmentDefinition.Value,
                        DirectiveLocation_FragmentDefinition,
                        DirectiveLocation.FragmentDefinition),
                    new(Lang.FragmentSpread.Value,
                        DirectiveLocation_FragmentSpread,
                        DirectiveLocation.FragmentSpread),
                    new(Lang.InlineFragment.Value,
                        DirectiveLocation_InlineFragment,
                        DirectiveLocation.InlineFragment),
                    new(Lang.VariableDefinition.Value,
                        DirectiveLocation_VariableDefinition,
                        DirectiveLocation.VariableDefinition),
                    new(Lang.Schema.Value,
                        DirectiveLocation_Schema,
                        DirectiveLocation.Schema),
                    new(Lang.Scalar.Value,
                        DirectiveLocation_Scalar,
                        DirectiveLocation.Scalar),
                    new(Lang.Object.Value,
                        DirectiveLocation_Object,
                        DirectiveLocation.Object),
                    new(Lang.FieldDefinition.Value,
                        DirectiveLocation_FieldDefinition,
                        DirectiveLocation.FieldDefinition),
                    new(Lang.ArgumentDefinition.Value,
                        DirectiveLocation_ArgumentDefinition,
                        DirectiveLocation.ArgumentDefinition),
                    new(Lang.Interface.Value,
                        DirectiveLocation_Interface,
                        DirectiveLocation.Interface),
                    new(Lang.Union.Value,
                        DirectiveLocation_Union,
                        DirectiveLocation.Union),
                    new(Lang.Enum.Value,
                        DirectiveLocation_Enum,
                        DirectiveLocation.Enum),
                    new(Lang.EnumValue.Value,
                        DirectiveLocation_EnumValue,
                        DirectiveLocation.EnumValue),
                    new(Lang.InputObject.Value,
                        DirectiveLocation_InputObject,
                        DirectiveLocation.InputObject),
                    new(Lang.InputFieldDefinition.Value,
                        DirectiveLocation_InputFieldDefinition,
                        DirectiveLocation.InputFieldDefinition),
            }
        };

    public static class Names
    {
        public const string __DirectiveLocation = "__DirectiveLocation";
    }
}
#pragma warning restore IDE1006 // Naming Styles
