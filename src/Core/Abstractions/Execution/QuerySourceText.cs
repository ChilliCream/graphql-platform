using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    public class QuerySourceText
        : IQuery
    {
        private byte[] _source;

        public QuerySourceText(string sourceText)
        {
            Text = sourceText
                ?? throw new ArgumentNullException(nameof(sourceText));
        }

        public string Text { get; }

        public void WriteTo(Stream output)
        {
            var writer = new StreamWriter(output, Encoding.UTF8);
            writer.Write(Text);
            writer.Flush();
        }

        public Task WriteToAsync(Stream output) =>
            WriteToAsync(output, CancellationToken.None);

        public async Task WriteToAsync(
            Stream output,
            CancellationToken cancellationToken)
        {
            var writer = new StreamWriter(output, Encoding.UTF8);
            await writer.WriteAsync(Text).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }

        public ReadOnlySpan<byte> ToSpan()
        {
            if (_source == null)
            {
                _source = Encoding.UTF8.GetBytes(Text);
            }
            return _source;
        }

        public override string ToString() => Text;
    }
}
