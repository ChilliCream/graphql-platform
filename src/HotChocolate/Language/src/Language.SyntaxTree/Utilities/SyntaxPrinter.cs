using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Language.Utilities;

/// <summary>
/// This helper can serialize a GraphQL syntax tree into its string representation.
/// </summary>
public static class SyntaxPrinter
{
    private static readonly SyntaxSerializer _serializer = new(new() { Indented = true, });
    private static readonly SyntaxSerializer _serializerNoIndent = new(new() { Indented = false, });

    /// <summary>
    /// Prints a GraphQL syntax node`s string representation.
    /// </summary>
    /// <param name="node">The syntax node that shall be printed.</param>
    /// <param name="indented">Specified if the printed string shall have indentations.</param>
    /// <returns>
    /// Returns the printed GraphQL syntax tree.
    /// </returns>
    public static string Print(this ISyntaxNode node, bool indented = true)
    {
        var writer = StringSyntaxWriter.Rent();

        try
        {
            if (indented)
            {
                _serializer.Serialize(node, writer);
            }
            else
            {
                _serializerNoIndent.Serialize(node, writer);
            }

            return writer.ToString();
        }
        finally
        {
            StringSyntaxWriter.Return(writer);
        }
    }

    /// <summary>
    /// Prints a GraphQL syntax node`s string representation into a stream.
    /// </summary>
    /// <param name="node">The syntax node that shall be printed.</param>
    /// <param name="stream">The stream to which the printed node shall be written to.</param>
    /// <param name="indented">Specified if the printed string shall have indentations.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// Returns the printed GraphQL syntax tree.
    /// </returns>
    public static async ValueTask PrintToAsync(
        this ISyntaxNode node,
        Stream stream,
        bool indented = true,
        CancellationToken cancellationToken = default)
    {
#if NETSTANDARD2_0
        using var streamWriter = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
#else
        await using var streamWriter = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
#endif

        var syntaxWriter = StringSyntaxWriter.Rent();

        try
        {
            if (indented)
            {
                _serializer.Serialize(node, syntaxWriter);
            }
            else
            {
                _serializerNoIndent.Serialize(node, syntaxWriter);
            }

#if NETSTANDARD2_0 || NETSTANDARD2_1
                await streamWriter
                    .WriteAsync(syntaxWriter.ToString())
                    .ConfigureAwait(false);
#else
            await streamWriter
                .WriteAsync(syntaxWriter.StringBuilder, cancellationToken)
                .ConfigureAwait(false);
#endif
        }
        finally
        {
            StringSyntaxWriter.Return(syntaxWriter);
        }
    }
}
