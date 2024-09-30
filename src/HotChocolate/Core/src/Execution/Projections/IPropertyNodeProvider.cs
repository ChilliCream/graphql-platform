using System.Reflection;

namespace HotChocolate.Execution.Projections;

internal interface IPropertyNodeProvider
{
    IReadOnlyList<PropertyNode> Nodes { get; }

    PropertyNode AddOrGetNode(PropertyInfo property);

    void TryAddNode(PropertyNode newNode);
}
