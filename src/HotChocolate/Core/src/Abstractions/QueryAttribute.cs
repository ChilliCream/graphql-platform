namespace HotChocolate;

/// <summary>
/// Marks a public/internal static method or property as a query root field.
/// The Hot Chocolate source generator will collect these and merge them into
/// the query type.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class QueryAttribute : Attribute;
