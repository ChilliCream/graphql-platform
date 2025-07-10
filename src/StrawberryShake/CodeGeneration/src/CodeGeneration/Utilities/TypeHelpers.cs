using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities;

internal static class TypeHelpers
{
    public static bool DoesTypeApply(IType typeCondition, ITypeDefinition current)
    {
        return typeCondition.NamedType().IsAssignableFrom(current);
    }
}
