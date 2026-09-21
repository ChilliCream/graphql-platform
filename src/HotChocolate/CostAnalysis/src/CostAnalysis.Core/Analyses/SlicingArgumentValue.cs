using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// A slicing argument's supplied value and schema default.
/// </summary>
/// <param name="SuppliedValue">
/// The value supplied for this argument in the operation, or
/// <see langword="null"/> when the argument was omitted.
/// </param>
/// <param name="SchemaDefaultValue">
/// The argument's schema-declared default value, or <see langword="null"/>
/// when it has none.
/// </param>
internal readonly record struct SlicingArgumentValue(
    IValueNode? SuppliedValue,
    IValueNode? SchemaDefaultValue);
