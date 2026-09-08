using HotChocolate.Language;

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
/// One argument or input field captured by the schema snapshot.
/// </summary>
/// <param name="Name">
/// The input value's name.
/// </param>
/// <param name="Weight">
/// The input value's own weight.
/// </param>
/// <param name="TypeName">
/// The named input type.
/// </param>
/// <param name="DefaultValue">
/// The schema default, or <see langword="null"/> when none is declared.
/// </param>
internal readonly record struct InputValueMetadata(
    string Name,
    double Weight,
    string TypeName,
    IValueNode? DefaultValue);

/// <summary>
/// One directive definition argument's own weight and default presence.
/// </summary>
internal readonly record struct DirectiveArgumentDefinition(string Name, double Weight, bool HasDefaultValue);
