using System;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal static class TypeSystemMemberExtensions
    {
        public static bool IsType(this ITypeSystemMember typeSystemMember) =>
            typeSystemMember is IType;

        public static IType Type(this ITypeSystemMember typeSystemMember)
        {
            if (typeSystemMember is IType type)
            {
                return type;
            }

            // TODO : Resources
            throw new ArgumentException(
                "TypeResources.TypeExtensions_InvalidStructure");
        }
    }
}
