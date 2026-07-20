namespace HotChocolate.Fusion.Execution.Nodes;

internal delegate void ResolveFieldValue(
    FieldContext context);

internal delegate ValueTask AsyncResolveFieldValue(
    FieldContext context);
