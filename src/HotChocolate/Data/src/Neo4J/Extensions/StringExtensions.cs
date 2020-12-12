using System;

namespace HotChocolate.Data.Neo4J.Extensions
{
    public static class StringExtensions
    {
        public static bool HasText(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public static bool IsIdentifier(this string str)
        {
            if (str.Length == 0)
                return false;

            return true;
        }
    }
}
