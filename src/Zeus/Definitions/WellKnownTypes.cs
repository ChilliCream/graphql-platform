using System;

namespace Zeus.Definitions
{
    public static class WellKnownTypes
    {
        public static readonly string Query = "Query";
        public static readonly string Mutation = "Mutation";

        public static bool IsQuery(string typeName)
        {
            return Query.Equals(typeName, StringComparison.Ordinal);
        }

        public static bool IsMutation(string typeName)
        {
            return Mutation.Equals(typeName, StringComparison.Ordinal);
        }
    }
}