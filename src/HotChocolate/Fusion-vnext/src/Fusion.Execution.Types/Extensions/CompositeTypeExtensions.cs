using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

public static class CompositeTypeExtensions
{
    public static bool IsEntity(this ITypeDefinition type)
    {
        if (type is FusionObjectTypeDefinition objectType)
        {
            return objectType.IsEntity;
        }

        return false;
    }
}
