namespace HotChocolate;

/// <summary>
/// Marks a public/internal static method or property as a mutation root field.
/// The Hot Chocolate source generator will collect these and merge them into
/// the mutation type.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class MutationAttribute : Attribute;
