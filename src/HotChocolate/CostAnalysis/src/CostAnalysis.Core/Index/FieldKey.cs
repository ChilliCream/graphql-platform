namespace HotChocolate.CostAnalysis;

/// <summary>
/// Keys one type's field in the schema index's per-field metadata indices (output
/// field weights, <c>@listSize</c> metadata, input field weights).
/// </summary>
internal readonly record struct FieldKey(string TypeName, string FieldName);
