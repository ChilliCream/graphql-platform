namespace HotChocolate.Language
{
    internal static class StringExtensions
    {
        public static string NormalizeLineBreaks(this string s)
        {
            return s.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
