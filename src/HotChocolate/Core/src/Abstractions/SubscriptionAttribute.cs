namespace HotChocolate;

/// <summary>
/// Marks a public/internal static method or property as a subscription root field.
/// The Hot Chocolate source generator will collect these and merge them into
/// the subscription type.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class SubscriptionAttribute : Attribute;
