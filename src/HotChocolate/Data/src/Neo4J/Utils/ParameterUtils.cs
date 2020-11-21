using System.Text.RegularExpressions;

namespace HotChocolate.Data.Neo4J.Utils
{
    public static class ParameterUtils
    {
        public static string UniqueString(string str, string[] existing)
        {
            string camelCasedString = str.ToCamelCase();
            int number;

            var regex = new Regex("/[0-9]+$/");

            var matches = regex.Match(camelCasedString);
            if (matches.Success)
            {
                number = matches.Value[0] + 1;
                camelCasedString = camelCasedString.Substring(0, camelCasedString.Length - matches.Value[0].)
            }
        }
    }
}