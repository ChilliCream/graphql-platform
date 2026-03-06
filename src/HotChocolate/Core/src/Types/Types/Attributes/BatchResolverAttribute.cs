namespace HotChocolate.Types;

/// <summary>
/// Marks a method as a batch resolver. A batch resolver receives lists of
/// parent objects and arguments, resolves them in a single invocation, and
/// returns a list of results — one per parent.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BatchResolverAttribute : Attribute;
