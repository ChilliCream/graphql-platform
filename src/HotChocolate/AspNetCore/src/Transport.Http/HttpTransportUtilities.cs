using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

internal static class HttpTransportUtilities
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;
    private static readonly int s_utf8CodePage = s_utf8.CodePage;

    public static Encoding? GetEncoding(string? charset)
    {
        Encoding? encoding = null;

        if (charset != null)
        {
            try
            {
                // Remove at most a single set of quotes.
                if (charset.Length > 2 && charset[0] == '\"' && charset[^1] == '\"')
                {
                    encoding = Encoding.GetEncoding(charset[1..^1]);
                }
                else
                {
                    encoding = Encoding.GetEncoding(charset);
                }
            }
            catch (ArgumentException e)
            {
                throw new InvalidOperationException("Invalid Charset", e);
            }

            Debug.Assert(encoding != null);
        }

        return encoding;
    }

    public static bool NeedsTranscoding([NotNullWhen(true)] Encoding? sourceEncoding)
        => sourceEncoding is not null
            && sourceEncoding.CodePage != s_utf8CodePage;

    public static Stream GetTranscodingStream(Stream contentStream, Encoding sourceEncoding)
        => Encoding.CreateTranscodingStream(
            contentStream,
            innerStreamEncoding: sourceEncoding,
            outerStreamEncoding: s_utf8);
}
