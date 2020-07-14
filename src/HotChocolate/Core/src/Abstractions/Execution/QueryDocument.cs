using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Execution
{
    public class QueryDocument
        : IQuery
    {
        public QueryDocument(DocumentNode document)
        {
            Document = document
                ?? throw new ArgumentNullException(nameof(document));
        }

        public DocumentNode Document { get; }

        public void WriteTo(Stream output)
        {
            using (var sw = new StreamWriter(output))
            {
                QuerySyntaxSerializer.Serialize(Document, sw, false);
                sw.Flush();
            }
        }

        public Task WriteToAsync(Stream output) =>
            WriteToAsync(output, CancellationToken.None);

        public async Task WriteToAsync(
            Stream output,
            CancellationToken cancellationToken)
        {
            using (var sw = new StreamWriter(output))
            {
                await Task.Run(() =>
                    QuerySyntaxSerializer.Serialize(Document, sw, false))
                    .ConfigureAwait(false);
                await sw.FlushAsync().ConfigureAwait(false);
            }
        }

        public ReadOnlySpan<byte> AsSpan()
        {
            using (var stream = new MemoryStream())
            {
                WriteTo(stream);
                return stream.ToArray();
            }
        }

        public override string ToString() =>
            QuerySyntaxSerializer.Serialize(Document, false);
    }
}
