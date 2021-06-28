using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;

namespace HotChocolate.Execution
{
    public class QueryDocument : IQuery
    {
        public QueryDocument(DocumentNode document)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }

        public DocumentNode Document { get; }

        public void WriteTo(Stream output)
        {
            using var sw = new StreamWriter(output);
            sw.Write(Document.Print(false));
            sw.Flush();
        }

        public Task WriteToAsync(Stream output) =>
            WriteToAsync(output, CancellationToken.None);

        public async Task WriteToAsync(
            Stream output,
            CancellationToken cancellationToken)
        {
            await Document
                .PrintToAsync(output, false, cancellationToken)
                .ConfigureAwait(false);
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            using var stream = new MemoryStream();
            using var sw = new StreamWriter(stream);

            sw.Write(Document.Print(false));
            sw.Flush();

            return stream.ToArray();
        }

        public override string ToString() =>
            Document.Print(true);
    }
}
