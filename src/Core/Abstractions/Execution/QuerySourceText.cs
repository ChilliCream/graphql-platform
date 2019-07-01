using System;
using System.Text;

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

        public ReadOnlySpan<byte> ToSource()
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
