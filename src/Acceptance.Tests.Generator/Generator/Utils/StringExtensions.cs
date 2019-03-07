namespace Generator
{
    internal static class StringExtensions
    {
        internal static string UpperFirstLetter(this string value)
        {
            char[] items = value.ToCharArray();
            items[0] = char.ToUpper(items[0]);
            return new string(items);
        }
    }
}