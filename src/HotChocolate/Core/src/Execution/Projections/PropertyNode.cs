#if NET6_0_OR_GREATER
using System.Collections.Immutable;
using System.Reflection;

namespace HotChocolate.Execution.Projections;

internal sealed class PropertyNode(
    PropertyInfo property,
    ImmutableArray<PropertyNode> nodes)
{
    public PropertyInfo Property { get; } = property;

    public ImmutableArray<PropertyNode> Nodes { get; } = nodes;
}
#endif