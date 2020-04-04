using HotChocolate.Types;

namespace HotChocolate.Utilities
{
    internal static class TypeFactoryHelper
    {
        public static INamedType PlaceHolder { get; } = new StringType();

        public static bool IsTypeStructureValid(IType type)
        {
            if (type.Depth() > 6)
            {
                return false;
            }

            return IsTypeStructureValid(type, 0);
        }

        public static bool IsTypeStructureValid(IType type, int listCount)
        {
            if (type is NonNullType nnt)
            {
                return IsTypeStructureValid(nnt.Type, listCount);
            }

            if (type is ListType lt)
            {
                if (listCount > 1)
                {
                    return false;
                }
                return IsTypeStructureValid(lt.ElementType, listCount + 1);
            }

            return true;
        }
    }
}
