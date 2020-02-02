using HotChocolate.Types;

namespace StrawberryShake.Generators.Utilities
{
    internal static class TypeHelpers
    {
        internal static bool DoesTypeApply(IType typeCondition, INamedType current)
        {
            if (typeCondition is ObjectType ot)
            {
                return ot == current;
            }
            else if (typeCondition is InterfaceType it)
            {
                if (current is ObjectType cot)
                {
                    return cot.Interfaces.ContainsKey(it.Name);
                }

                if (current is InterfaceType cit)
                {
                    return it.Name.Equals(cit.Name);
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
