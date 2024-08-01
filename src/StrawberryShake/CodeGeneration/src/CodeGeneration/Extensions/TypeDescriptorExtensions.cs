using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.Extensions;

public static class TypeDescriptorExtensions
{
    public static bool IsLeaf(this ITypeDescriptor typeDescriptor) =>
        typeDescriptor.Kind == TypeKind.Leaf;

    public static bool IsEntity(this ITypeDescriptor typeDescriptor) =>
        typeDescriptor.Kind == TypeKind.Entity;

    public static bool IsData(this ITypeDescriptor typeDescriptor) =>
        typeDescriptor.Kind == TypeKind.Data ||
        typeDescriptor.Kind == TypeKind.AbstractData;

    public static bool ContainsEntity(this ITypeDescriptor typeDescriptor)
    {
        return typeDescriptor switch
        {
            ListTypeDescriptor listTypeDescriptor =>
                listTypeDescriptor.InnerType.ContainsEntity(),
            ComplexTypeDescriptor namedTypeDescriptor =>
                namedTypeDescriptor.Properties
                    .Any(prop => prop.Type.IsEntity() || prop.Type.ContainsEntity()),
            NonNullTypeDescriptor nonNullTypeDescriptor =>
                nonNullTypeDescriptor.InnerType.ContainsEntity(),
            _ => false,
        };
    }

    public static bool IsOrContainsEntity(this ITypeDescriptor typeDescriptor)
    {
        return typeDescriptor switch
        {
            ListTypeDescriptor d => d.InnerType.IsOrContainsEntity(),

            InterfaceTypeDescriptor d =>
                d.IsEntity() ||
                d.Properties.Any(p =>
                    p.Type.IsEntity() || p.Type.IsOrContainsEntity()) ||
                d.ImplementedBy.Any(x => x.IsOrContainsEntity()),

            ComplexTypeDescriptor d =>
                d.IsEntity() ||
                d.Properties.Any(p => p.Type.IsEntity() || p.Type.IsOrContainsEntity()),

            NonNullTypeDescriptor d => d.InnerType.IsOrContainsEntity(),

            _ => false,
        };
    }

    public static bool IsInterface(this ITypeDescriptor typeDescriptor) =>
        typeDescriptor is InterfaceTypeDescriptor;

    public static bool IsNullable(this ITypeDescriptor typeDescriptor) =>
        typeDescriptor is not NonNullTypeDescriptor;

    public static bool IsNonNull(this ITypeDescriptor typeDescriptor) =>
        typeDescriptor is NonNullTypeDescriptor;

    public static bool IsList(this ITypeDescriptor typeDescriptor) =>
        typeDescriptor is ListTypeDescriptor ||
        typeDescriptor is NonNullTypeDescriptor { InnerType: ListTypeDescriptor, };

    public static ITypeDescriptor InnerType(this ITypeDescriptor typeDescriptor)
    {
        if (typeDescriptor is NonNullTypeDescriptor n)
        {
            return n.InnerType;
        }

        if (typeDescriptor is ListTypeDescriptor l)
        {
            return l.InnerType;
        }

        return typeDescriptor;
    }

    public static ITypeDescriptor NamedType(this ITypeDescriptor typeDescriptor)
    {
        return typeDescriptor
            .InnerType()
            .InnerType()
            .InnerType()
            .InnerType()
            .InnerType()
            .InnerType();
    }

    public static string GetName(this ITypeDescriptor typeDescriptor)
    {
        if (typeDescriptor.NamedType() is INamedTypeDescriptor namedType)
        {
            return namedType.Name;
        }

        throw new InvalidOperationException("Invalid type structure.");
    }

    public static RuntimeTypeInfo GetRuntimeType(this ITypeDescriptor typeDescriptor)
    {
        if (typeDescriptor.NamedType() is INamedTypeDescriptor namedType)
        {
            return namedType.RuntimeType;
        }

        throw new InvalidOperationException("Invalid type structure.");
    }
}
