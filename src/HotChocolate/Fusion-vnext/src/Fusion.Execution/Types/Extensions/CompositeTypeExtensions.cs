namespace HotChocolate.Fusion.Types;

public static class CompositeTypeExtensions
{
    public static ICompositeNamedType NamedType(this ICompositeType type)
    {
        switch (type)
        {
            case CompositeNonNullType nonNullType:
                return NamedType(nonNullType.Type);

            case CompositeListType listType:
                return NamedType(listType.Type);

            case ICompositeNamedType compositeNamedType:
                return compositeNamedType;

            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public static bool IsEntity(this ICompositeNamedType type)
    {
        return true;
    }
}
