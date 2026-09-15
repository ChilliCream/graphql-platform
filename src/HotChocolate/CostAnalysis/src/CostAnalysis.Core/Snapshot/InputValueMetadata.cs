using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

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
