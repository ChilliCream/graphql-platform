namespace HotChocolate.CostAnalysis;

/// <summary>
/// Keys one type's field in the snapshot's per-field metadata indices (output
/// field weights, <c>@listSize</c> metadata, input field weights).
/// </summary>
internal readonly record struct FieldKey(string TypeName, string FieldName);

/// <summary>
/// Keys one output field's argument in the snapshot's argument-weight index.
/// </summary>
internal readonly record struct ArgumentKey(string TypeName, string FieldName, string ArgumentName);

/// <summary>
/// Keys one directive definition's argument in the snapshot's
/// directive-argument-weight index.
/// </summary>
internal readonly record struct DirectiveArgumentKey(string DirectiveName, string ArgumentName);
