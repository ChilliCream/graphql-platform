using System;
using System.Collections;
using System.Reflection;

namespace HotChocolate.Data.Neo4J.Extensions
{
    public static class PropertyInfoExtensions
    {
        public static bool HasAttribute(this PropertyInfo prop, Type t)
        {
            return prop.GetCustomAttribute(t) != null;
        }

        public static bool IsNullableLong(this PropertyInfo prop)
        {
            return prop.PropertyType == typeof(long?);
        }

        public static bool IsDateTime(this PropertyInfo prop)
        {
            return prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime);
        }

        public static bool IsCollection(this PropertyInfo prop)
        {
            return prop.PropertyType.GetInterface(nameof(ICollection)) != null;
        }
    }
}
