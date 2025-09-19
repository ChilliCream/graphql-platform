namespace StrawberryShake.CodeGeneration.CSharp;

internal static class StringExtensions
{
    public static string AsStringToken(this string str)
    {
        return "\"" + str + "\"";
    }

    public static string WithGeneric(this string str, params string[] generics)
    {
        return str + "<" + string.Join(", ", generics) + ">";
    }

    public static string WithGeneric(this string str, params RuntimeTypeInfo[] generics)
    {
        return str + "<" + string.Join(", ", generics.Select(x => x.ToString())) + ">";
    }

    public static string MakeNullable(this string str, bool isNullable = true)
    {
        if (isNullable)
        {
            return str + "?";
        }

        return str;
    }
}
