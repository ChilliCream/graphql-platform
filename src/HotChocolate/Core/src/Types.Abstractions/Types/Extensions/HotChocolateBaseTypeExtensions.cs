#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130

public static class HotChocolateBaseTypeExtensions
{
    public static ITypeDefinition AsTypeDefinition(this IType type)
        => type.Kind switch
        {
            TypeKind.NonNull => AsTypeDefinition(((NonNullType)type).NullableType),
            TypeKind.List => AsTypeDefinition(((ListType)type).ElementType),
            TypeKind.Object or
                TypeKind.Interface or
                TypeKind.Union or
                TypeKind.InputObject or
                TypeKind.Enum or
                TypeKind.Scalar => (ITypeDefinition)type,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

    public static bool Equals(this IType thisType, IType? otherType, TypeComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(thisType, nameof(thisType));

        if(otherType is null)
        {
            return false;
        }

        if (comparison is TypeComparison.Reference)
        {
            return ReferenceEquals(thisType, otherType);
        }

        return thisType.IsStructurallyEqual(otherType);
    }

    private static bool IsStructurallyEqual(this IType thisType, IType otherType)
    {
        var depthLimit = 16;

        while (true)
        {
            if (depthLimit-- <= 0)
            {
                throw new InvalidOperationException("The type comparison depth limit was reached.");
            }

            if (thisType.Kind != otherType.Kind)
            {
                return false;
            }

            if (thisType.Kind == TypeKind.NonNull)
            {
                thisType = ((NonNullType)thisType).NullableType;
                otherType = ((NonNullType)otherType).NullableType;
                continue;
            }

            if (thisType.Kind == TypeKind.List)
            {
                thisType = ((ListType)thisType).ElementType;
                otherType = ((ListType)otherType).ElementType;
                continue;
            }

            if (thisType.Kind == TypeKind.Object
                || thisType.Kind == TypeKind.Interface
                || thisType.Kind == TypeKind.Union
                || thisType.Kind == TypeKind.InputObject
                || thisType.Kind == TypeKind.Enum
                || thisType.Kind == TypeKind.Scalar)
            {
                return ((ITypeDefinition)thisType).Name.Equals(((ITypeDefinition)otherType).Name);
            }

            throw new InvalidOperationException("The specified type kind is not supported.");
        }
    }
}
