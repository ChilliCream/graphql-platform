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
/// One directive definition argument's own weight and whether it declares a
/// schema default, the shape the snapshot's directive-argument index stores
/// per directive name.
/// </summary>
/// <param name="Name">
/// The argument's name.
/// </param>
/// <param name="Weight">
/// The argument's own weight.
/// </param>
/// <param name="HasDefaultValue">
/// Whether the argument declares a schema default value.
/// </param>
internal readonly record struct DirectiveArgumentDefinition(string Name, double Weight, bool HasDefaultValue);
