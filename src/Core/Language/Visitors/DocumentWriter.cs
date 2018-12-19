using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HotChocolate.Language
{
    public class DocumentWriter
        : TextWriter
    {
        private TextWriter _writer;

        public DocumentWriter(TextWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
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
            Write(' ');
        }

        public void WriteIndentation()
        {
            if (Indentation > 0)
            {
                Write(new string(' ', Indentation * 2));
            }
        }

        public Task WriteSpaceAsync()
        {
            return WriteAsync(' ');
        }

        public Task WriteIndentationAsync()
        {
            if (Indentation > 0)
            {
                return WriteAsync(new string(' ', Indentation * 2));
            }
            return Task.CompletedTask;
        }
    }
}
