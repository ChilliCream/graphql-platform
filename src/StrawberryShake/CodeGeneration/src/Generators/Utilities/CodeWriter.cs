using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace StrawberryShake.Generators.Utilities
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

        public override Encoding Encoding { get; } = Encoding.UTF8;

        public override void Write(char value) =>
            _writer.Write(value);

        public void WriteStringValue(string value)
        {
            Write('"');
            Write(value);
            Write('"');
        }

        public Task WriteStringValueAsync(string value) =>
            Task.Factory.StartNew(
                () => WriteStringValue(value),
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

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

        public string GetIndentString()
        {
            if (_indent > 0)
            {
                return new string(' ', _indent * 4);
            }
            return string.Empty;
        }

        public Task WriteIndentAsync() =>
            Task.Factory.StartNew(
                WriteIndent,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

        public Task WriteIndentedLineAsync(string format, params object?[] args) =>
            Task.Factory.StartNew(
                () =>
                {
                    WriteIndent();
                    if (args.Length == 0)
                    {
                        Write(format);
                    }
                    else
                    {
                        Write(format, args);
                    }
                    WriteLine();
                },
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

        public IDisposable IncreaseIndent()
        {
            _indent++;
            return new Block(DecreaseIndent);
        }

        public void DecreaseIndent()
        {
            if (_indent > 0)
            {
                _indent--;
            }
        }

        public IDisposable WriteBraces()
        {
            WriteLeftBrace();
            WriteLine();
            IncreaseIndent();

            return new Block(() =>
            {
                WriteLine();
                DecreaseIndent();
                WriteIndent();
                WriteRightBrace();
            });
        }

        public void WriteLeftBrace() => Write('{');

        public Task WriteLeftBraceAsync() =>
            Task.Factory.StartNew(
                WriteLeftBrace,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

        public void WriteRightBrace() => Write('}');

        public Task WriteRightBraceAsync() =>
            Task.Factory.StartNew(
                WriteRightBrace,
                CancellationToken.None,
                TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);

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

        private class Block
            : IDisposable
        {
            private Action _decrease;

            public Block(Action close)
            {
                _decrease = close;
            }

            public void Dispose() => _decrease();
        }
    }
}
