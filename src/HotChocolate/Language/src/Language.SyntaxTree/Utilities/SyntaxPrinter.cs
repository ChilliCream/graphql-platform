using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Language.Utilities
{
    public static class SyntaxPrinter
    {
        private static readonly SyntaxSerializer _serializer =
            new(new SyntaxSerializerOptions { Indented = true });
        private static readonly SyntaxSerializer _serializerNoIndent =
            new(new SyntaxSerializerOptions { Indented = false });

        public static string Print(this ISyntaxNode node, bool indented = true)
        {
            StringSyntaxWriter writer = StringSyntaxWriter.Rent();

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

        public static async ValueTask PrintToAsync(
            this ISyntaxNode node,
            Stream stream,
            bool indented = true,
            CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_0
            using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
#else
            var streamWriter = new StreamWriter(stream, Encoding.UTF8);
            await using (streamWriter.ConfigureAwait(false));
#endif

            StringSyntaxWriter syntaxWriter = StringSyntaxWriter.Rent();

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
}
