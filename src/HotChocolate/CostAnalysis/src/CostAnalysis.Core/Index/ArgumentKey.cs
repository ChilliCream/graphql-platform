namespace HotChocolate.CostAnalysis;

/// <summary>
/// Keys one output field's argument in the schema index's argument-weight index.
/// </summary>
internal readonly record struct ArgumentKey(string TypeName, string FieldName, string ArgumentName);
