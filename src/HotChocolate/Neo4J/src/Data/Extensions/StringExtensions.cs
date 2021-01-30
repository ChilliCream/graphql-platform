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
            return str.Length != 0;
        }
    }
}
