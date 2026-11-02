namespace HotChocolate.CostAnalysis;

/// <summary>
/// One directive definition argument's own weight and default presence.
/// </summary>
internal readonly record struct DirectiveArgumentDefinition(string Name, double Weight, bool HasDefaultValue);
