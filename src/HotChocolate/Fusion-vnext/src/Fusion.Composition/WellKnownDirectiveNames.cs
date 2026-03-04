using HotChocolate.Types;

namespace HotChocolate.Fusion;

internal static class WellKnownDirectiveNames
{
    public const string CacheControl = DirectiveNames.CacheControl.Name;
    public const string Cost = DirectiveNames.Cost.Name;
    public const string External = DirectiveNames.External.Name;
    public const string FusionCost = "fusion__cost";
    public const string FusionEnumValue = "fusion__enumValue";
    public const string FusionField = "fusion__field";
    public const string FusionImplements = "fusion__implements";
    public const string FusionInaccessible = "fusion__inaccessible";
    public const string FusionInputField = "fusion__inputField";
    public const string FusionListSize = "fusion__listSize";
    public const string FusionLookup = "fusion__lookup";
    public const string FusionRequires = "fusion__requires";
    public const string FusionSchemaMetadata = "fusion__schema_metadata";
    public const string FusionType = "fusion__type";
    public const string FusionUnionMember = "fusion__unionMember";
    public const string Inaccessible = DirectiveNames.Inaccessible.Name;
    public const string Internal = DirectiveNames.Internal.Name;
    public const string Is = DirectiveNames.Is.Name;
    public const string Key = DirectiveNames.Key.Name;
    public const string ListSize = DirectiveNames.ListSize.Name;
    public const string Lookup = DirectiveNames.Lookup.Name;
    public const string McpToolAnnotations = "mcpToolAnnotations";
    public const string OneOf = DirectiveNames.OneOf.Name;
    public const string Override = DirectiveNames.Override.Name;
    public const string Provides = DirectiveNames.Provides.Name;
    public const string Require = DirectiveNames.Require.Name;
    public const string SerializeAs = DirectiveNames.SerializeAs.Name;
    public const string Shareable = DirectiveNames.Shareable.Name;
    public const string Tag = DirectiveNames.Tag.Name;
}
