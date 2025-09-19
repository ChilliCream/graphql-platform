namespace HotChocolate.Types.Relay;

/// <summary>
/// This attribute marks the node resolver in a relay node type.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NodeResolverAttribute : Attribute;
