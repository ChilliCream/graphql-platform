using System;

namespace HotChocolate.Stitching.Merge
{
    public static class TypeInfoExtensions
    {
        public static bool IsQueryType(this ITypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return typeInfo.IsRootType
                && typeInfo.Definition == typeInfo.Schema.QueryType;
        }

        public static bool IsMutationType(this ITypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return typeInfo.IsRootType
                && typeInfo.Definition == typeInfo.Schema.MutationType;
        }

        public static bool IsSubscriptionType(this ITypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return typeInfo.IsRootType
                && typeInfo.Definition == typeInfo.Schema.SubscriptionType;
        }
    }
}
