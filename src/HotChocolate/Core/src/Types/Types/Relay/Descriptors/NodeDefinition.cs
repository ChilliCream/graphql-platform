using System.Reflection;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Relay.Descriptors;

/// <summary>
/// The node definition is a mutable object that is used during type initialization
/// to configure object types that implement <see cref="INode"/>.
/// </summary>
public class NodeDefinition : DefinitionBase
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
    public ObjectFieldDefinition? ResolverField { get; set; }
}
