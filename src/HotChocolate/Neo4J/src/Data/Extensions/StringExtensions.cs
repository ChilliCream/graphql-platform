namespace HotChocolate.Data.Neo4J.Extensions
{
    internal static class StringExtensions
    {
        public static bool HasText(this string str) => !string.IsNullOrEmpty(str);
    }
}
