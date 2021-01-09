using System;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal static class TypeHelpers
    {
        public static bool DoesTypeApply(IType typeCondition, INamedType current)
        {
            if (typeCondition is ObjectType ot)
            {
                return ot == current;
            }
            else if (typeCondition is InterfaceType it)
            {
                if (current is ObjectType cot)
                {
                    return cot.IsAssignableFrom(it);
                }

                if (current is InterfaceType cit)
                {
                    return it.Name.Equals(cit.Name, StringComparison.Ordinal);
                }
            }
            else if (typeCondition is UnionType ut)
            {
                return ut.Types.ContainsKey(current.Name);
            }
            return false;
        }
    }
}
