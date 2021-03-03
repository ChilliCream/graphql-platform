using System.Text;

namespace StrawberryShake
{
    public static class DocumentExtensions
    {
        public static string Print(this IDocument document) =>
            Encoding.UTF8.GetString(document.Body.ToArray());
    }
}
