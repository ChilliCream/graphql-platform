using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities;

internal static class TypeHelpers
{
    public static bool DoesTypeApply(IType typeCondition, INamedType current)
    {
        return typeCondition.NamedType().IsAssignableFrom(current);
    }
}
