using System.Reflection;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Relay.Descriptors;

/// <summary>
/// The node configuration is a mutable object used during type initialization
/// to configure object types that implement <see cref="INode"/>.
/// </summary>
public class NodeConfiguration : TypeSystemConfiguration
{
    /// <summary>
    /// Gets the node runtime type.
    /// </summary>
    public Type? NodeType { get; set; }

    /// <summary>
    /// Gets the node id member.
    /// </summary>
    public MemberInfo? IdMember { get; set; }

    /// <summary>
    /// Gets an object field definition representing the node resolver.
    /// </summary>
    public ObjectFieldConfiguration? ResolverField { get; set; }
}
