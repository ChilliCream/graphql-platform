using HotChocolate;

namespace StrawberryShake.CodeGeneration.CSharp
{
    internal static class StringExtensions
    {
        public static string AsStringToken(this NameString str)
        {
            return AsStringToken(str.Value);
        }

        public static string AsStringToken(this string str)
        {
            return "\"" + str + "\"";
        }

        public static string WithGeneric(this string str, params string[] generics)
        {
            return str + "<" + string.Join(", ", generics) + ">";
        }

        public static string MakeNullable(this string str)
        {
            return str + "?";
        }
    }
}
