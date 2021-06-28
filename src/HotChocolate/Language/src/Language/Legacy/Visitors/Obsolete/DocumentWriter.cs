using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HotChocolate.Language
{
    [Obsolete("This is replaced by the new schema printer.")]
    public class DocumentWriter : TextWriter
    {
        private const int _indent = 2;
        private const char _space = ' ';
        private readonly TextWriter _writer;

        public DocumentWriter(TextWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public DocumentWriter(StringBuilder stringBuilder)
        {
            if (stringBuilder == null)
            {
                throw new ArgumentNullException(nameof(stringBuilder));
            }
            _writer = new StringWriter(stringBuilder);
        }

        public int Indentation { get; private set; }

        public override Encoding Encoding => _writer.Encoding;

        public override void Write(char value)
        {
            _writer.Write(value);
        }

        public override void Flush()
        {
            _writer.Flush();
        }

        public void Indent()
        {
            Indentation++;
        }

        public void Unindent()
        {
            Indentation--;
        }

        public void WriteSpace()
        {
            Write(_space);
        }

        public void WriteIndentation()
        {
            if (Indentation > 0)
            {
                Write(new string(_space, Indentation * _indent));
            }
        }

        public Task WriteSpaceAsync()
        {
            return WriteAsync(_space);
        }

        public Task WriteIndentationAsync()
        {
            if (Indentation > 0)
            {
                return WriteAsync(new string(_space, Indentation * _indent));
            }
            return Task.CompletedTask;
        }
    }
}
