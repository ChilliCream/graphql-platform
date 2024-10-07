using System.Reflection;

namespace HotChocolate.Execution.Projections;

internal sealed class PropertyNode : TypeContainer
{
    public PropertyNode(PropertyInfo property, List<TypeNode>? nodes = null) : base(nodes)
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
        List<TypeNode>? nodes,
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
        List<TypeNode>? nodes = null;

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
