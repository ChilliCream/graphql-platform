using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One slicing argument's supplied and schema-default values, the inputs
/// <see cref="ListSizeResolver"/> needs to resolve that argument after
/// coercion.
/// </summary>
/// <param name="SuppliedValue">
/// The value supplied for this argument in the operation, or
/// <see langword="null"/> when the argument was omitted.
/// </param>
/// <param name="SchemaDefaultValue">
/// The argument's schema-declared default value, or <see langword="null"/>
/// when it has none.
/// </param>
internal readonly record struct SlicingArgumentValue(IValueNode? SuppliedValue, IValueNode? SchemaDefaultValue);
