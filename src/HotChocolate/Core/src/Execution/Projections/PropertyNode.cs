using System.Reflection;

namespace HotChocolate.Execution.Projections;

internal sealed class PropertyNode : PropertyNodeContainer
{
    public PropertyNode(PropertyInfo property, List<PropertyNode>? nodes = null)
        : base(nodes)
    {
        Property = property;
        IsArray = property.PropertyType.IsArray;

        if (IsArray)
        {
            ElementType = property.PropertyType.GetElementType();
        }
        else
        {
            var collectionType = GetCollectionType(property.PropertyType);
            if (collectionType != null)
            {
                IsCollection = true;
                ElementType = collectionType.GetGenericArguments()[0];
            }
            else
            {
                IsCollection = false;
                ElementType = null;
            }
        }
    }

    private PropertyNode(
        PropertyInfo property,
        List<PropertyNode>? nodes,
        bool isArray,
        bool isCollection,
        Type? elementType)
        : base(nodes)
    {
        Property = property;
        IsArray = isArray;
        IsCollection = isCollection;
        ElementType = elementType;
    }

    public PropertyInfo Property { get; }

    public bool IsCollection { get; }

    public bool IsArray { get; }

    public bool IsArrayOrCollection => IsArray || IsCollection;

    public Type? ElementType { get; }

    public PropertyNode Clone()
    {
        List<PropertyNode>? nodes = null;

        if (Nodes.Count > 0)
        {
            nodes = new(Nodes.Count);
            foreach (var node in Nodes)
            {
                nodes.Add(node.Clone());
            }
        }

        return new PropertyNode(Property, nodes, IsArray, IsCollection, ElementType);
    }

    private static Type? GetCollectionType(Type type)
    {
        if (type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            return type;
        }

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>))
            {
                return interfaceType;
            }
        }

        return null;
    }
}

internal class PropertyNodeContainer(
    List<PropertyNode>? nodes = null)
    : IPropertyNodeProvider
{
    private static readonly IReadOnlyList<PropertyNode> _emptyNodes = Array.Empty<PropertyNode>();
    private List<PropertyNode>? _nodes = nodes;
    private bool _sealed;

    public IReadOnlyList<PropertyNode> Nodes
        => _nodes ?? _emptyNodes;

    public PropertyNode AddOrGetNode(PropertyInfo property)
    {
        if (_sealed)
        {
            throw new InvalidOperationException("The property node container is sealed.");
        }

        _nodes ??= new();

        foreach (var node in Nodes)
        {
            if (node.Property.Name.Equals(property.Name))
            {
                return node;
            }
        }

        var newNode = new PropertyNode(property);
        _nodes.Add(newNode);
        return newNode;
    }

    public void AddNode(PropertyNode newNode)
    {
        if (_sealed)
        {
            throw new InvalidOperationException("The property node container is sealed.");
        }

        _nodes ??= new();

        foreach (var node in Nodes)
        {
            if (node.Property.Name.Equals(node.Property.Name))
            {
                throw new InvalidOperationException("Duplicate property.");
            }
        }

        _nodes.Add(newNode);
    }

    public void Seal()
    {
        if (!_sealed)
        {
            foreach (var node in Nodes)
            {
                node.Seal();
            }

            _sealed = true;
        }
    }
}

internal interface IPropertyNodeProvider
{
    IReadOnlyList<PropertyNode> Nodes { get; }

    PropertyNode AddOrGetNode(PropertyInfo property);

    void AddNode(PropertyNode newNode);
}
