using System.Collections.Frozen;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion;

internal static class FusionBuiltIns
{
    private static readonly MutableScalarTypeDefinition s_fieldSelectionMapType =
        MutableScalarTypeDefinition.Create(FieldSelectionMap);

    private static readonly MutableScalarTypeDefinition s_fieldSelectionSetType =
        MutableScalarTypeDefinition.Create(FieldSelectionSet);

    private static readonly MutableScalarTypeDefinition s_stringType = BuiltIns.String.Create();

    public static FrozenDictionary<string, MutableDirectiveDefinition> SourceSchemaDirectives { get; } =
        new HashSet<MutableDirectiveDefinition>(
        [
            new ExternalMutableDirectiveDefinition(),
            new InaccessibleMutableDirectiveDefinition(),
            new InternalMutableDirectiveDefinition(),
            new IsMutableDirectiveDefinition(s_fieldSelectionMapType),
            new KeyMutableDirectiveDefinition(s_fieldSelectionSetType),
            new LookupMutableDirectiveDefinition(),
            new OverrideMutableDirectiveDefinition(s_stringType),
            new ProvidesMutableDirectiveDefinition(s_fieldSelectionSetType),
            new RequireMutableDirectiveDefinition(s_fieldSelectionMapType),
            new ShareableMutableDirectiveDefinition()
        ]).ToFrozenDictionary(d => d.Name);

    public static FrozenDictionary<string, MutableScalarTypeDefinition> SourceSchemaScalars { get; } =
        new HashSet<MutableScalarTypeDefinition>([s_fieldSelectionMapType, s_fieldSelectionSetType])
            .ToFrozenDictionary(s => s.Name);

    public static bool IsBuiltInSourceSchemaScalar(string typeName)
    {
        return typeName switch
        {
            FieldSelectionMap or FieldSelectionSet => true,
            _ => false
        };
    }
}
