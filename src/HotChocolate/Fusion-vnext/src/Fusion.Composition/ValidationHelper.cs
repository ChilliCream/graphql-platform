using HotChocolate.Types;

namespace HotChocolate.Fusion;

internal sealed class ValidationHelper
{
    public static bool SameTypeShape(IType typeA, IType typeB)
    {
        while (true)
        {
            if (typeA is NonNullType && typeB is not NonNullType)
            {
                typeA = typeA.InnerType();

                continue;
            }

            if (typeB is NonNullType && typeA is not NonNullType)
            {
                typeB = typeB.InnerType();

                continue;
            }

            if (typeA is ListType || typeB is ListType)
            {
                if (typeA is not ListType || typeB is not ListType)
                {
                    return false;
                }

                typeA = typeA.InnerType();
                typeB = typeB.InnerType();

                continue;
            }

            return typeA.Equals(typeB, TypeComparison.Structural);
        }
    }
}
