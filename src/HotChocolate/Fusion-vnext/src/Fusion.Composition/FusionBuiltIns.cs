using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion;

internal static class FusionBuiltIns
{
    public static bool IsBuiltInSourceSchemaScalar(string typeName)
    {
        return typeName switch
        {
            FieldSelectionMap or FieldSelectionSet => true,
            _ => false
        };
    }
}
