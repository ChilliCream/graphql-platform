using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    // TODO : remove ?
    internal static class ReflectionHelper
    {
        public static string GetTypeName(Type type)
        {
            if (type.IsDefined(typeof(GraphQLNameAttribute)))
            {
                GraphQLNameAttribute name = type.GetCustomAttribute<GraphQLNameAttribute>();
                return name.Name;
            }
            return type.Name;
        }


    }
}
