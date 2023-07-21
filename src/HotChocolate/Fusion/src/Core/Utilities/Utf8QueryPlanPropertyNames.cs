namespace HotChocolate.Fusion.Planning;

internal static class Utf8QueryPlanPropertyNames
{
    public static ReadOnlySpan<byte> SelectionSetIdsProp => "selectionSetIds"u8;

    public static ReadOnlySpan<byte> PathProp => "path"u8;

    public static ReadOnlySpan<byte> SubgraphProp => "subgraph"u8;

    public static ReadOnlySpan<byte> DocumentProp => "document"u8;

    public static ReadOnlySpan<byte> SelectionSetIdProp => "selectionSetId"u8;

    public static ReadOnlySpan<byte> RequiresProp => "requires"u8;

    public static ReadOnlySpan<byte> ProvidesProp => "provides"u8;

    public static ReadOnlySpan<byte> VariableProp => "variable"u8;

    public static ReadOnlySpan<byte> ForwardedVariablesProp => "forwardedVariables"u8;
}