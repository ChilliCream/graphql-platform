using System;
using System.Linq;

namespace HotChocolate.Client.Core.Utilities
{
    public static class StringExtensions
    {
        public static string LowerFirstCharacter(this string s)
        {
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string SnakeCaseToPascalCase(this string str)
        {
            var tokens = str.Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                tokens[i] = token.Substring(0, 1).ToUpper() + token.Substring(1).ToLower();
            }

            return string.Join("", tokens);
        }

        public static string PascalCaseToSnakeCase(this string s)
        {
            return string.Concat(s.ToCharArray()
                .Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToUpper();
        }
    }
}
