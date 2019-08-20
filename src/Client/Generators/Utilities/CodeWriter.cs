using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace StrawberryShake.Generators
{
    public class CodeWriter
        : TextWriter
    {
        private readonly TextWriter _writer;
        private readonly bool _disposeWriter;
        private bool _disposed;
        private int _indent = 0;

        public CodeWriter(TextWriter writer)
        {
            _writer = writer;
            _disposeWriter = false;
        }

        public CodeWriter(StringBuilder text)
        {
            _writer = new StringWriter(text);
            _disposeWriter = true;
        }

        public override Encoding Encoding { get; }

        public override void Write(char value) =>
            _writer.Write(value);

        public void WriteIndent()
        {
            if (_indent > 0)
            {
                int spaces = _indent * 4;
                for (int i = 0; i < spaces; i++)
                {
                    Write(' ');
                }
            }
        }

        public Task WriteIndentAsync() =>
            Task.Factory.StartNew(
                WriteIndent,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

        public void WriteSpace() => Write(' ');

        public Task WriteSpaceAsync() =>
            Task.Factory.StartNew(
                WriteSpace,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

        public void IncreaseIndent() =>
            _indent++;

        public void DecreaseIndent()
        {
            if (_indent > 0)
            {
                _indent--;
            }
        }

        public override void Flush()
        {
            base.Flush();
            _writer.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed && _disposeWriter)
            {
                if (disposing)
                {
                    _writer.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
