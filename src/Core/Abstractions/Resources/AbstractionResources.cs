using System;
using System.Globalization;

namespace HotChocolate
{
    internal static class AbstractionResources
    {
        public static string Type_Name_IsNotValid(string typeName)
        {
            string name = typeName ?? "null";
            return $"`{name}` is not a valid " +
                "GraphQL type name.";
        }

        public static string Name_Cannot_BeEmpty()
        {
            return "The specified name cannot be empty.";
        }
    }
}
