using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    public interface ITypeInfo
    {
        ITypeDefinitionNode Definition { get; }

        ISchemaInfo Schema { get; }

        bool IsRootType { get; }
    }

    public interface ITypeInfo<out T>
        : ITypeInfo
        where T : ITypeDefinitionNode
    {
        new T Definition { get; }
    }

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
