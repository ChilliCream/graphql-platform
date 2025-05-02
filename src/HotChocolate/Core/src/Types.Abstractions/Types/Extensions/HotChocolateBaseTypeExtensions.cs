#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130

public static class HotChocolateBaseTypeExtensions
{
    private const int _maxDepth = 16;

    public static ITypeDefinition AsTypeDefinition(this IType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var depthRemaining = _maxDepth;

        while (true)
        {
            if (depthRemaining-- <= 0)
            {
                throw new InvalidOperationException($"The type resolution depth limit of {_maxDepth} was exceeded.");
            }

            switch (type.Kind)
            {
                case TypeKind.NonNull:
                    type = ((NonNullType)type).NullableType;
                    continue;

                case TypeKind.List:
                    type = ((ListType)type).ElementType;
                    continue;

                case TypeKind.Object:
                case TypeKind.Interface:
                case TypeKind.Union:
                case TypeKind.InputObject:
                case TypeKind.Enum:
                case TypeKind.Scalar:
                    return (ITypeDefinition)type;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }

    public static bool Equals(this IType thisType, IType? otherType, TypeComparison comparison)
    {
        ArgumentNullException.ThrowIfNull(thisType);

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
        var depthRemaining = _maxDepth;

        while (true)
        {
            if (depthRemaining-- <= 0)
            {
                throw new InvalidOperationException($"The type comparison depth limit of {_maxDepth} was reached.");
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
