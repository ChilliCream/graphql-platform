using System.Text;

namespace HotChocolate.Language.Utilities
{
    public class StringSyntaxWriter
        : ISyntaxWriter
    {
        private static StringSyntaxWriterPool _pool = new StringSyntaxWriterPool();
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private int _indent;

        public static StringSyntaxWriter Rent()
        {
            return _pool.Get();
        }

        public static void Return(StringSyntaxWriter writer)
        {
            _pool.Return(writer);
        }

        public void Indent()
        {
            _indent++;
        }

        public void Unindent()
        {
            if (_indent > 0)
            {
                _indent--;
            }
        }

        public void Write(char c)
        {
            _stringBuilder.Append(c);
        }

        public void Write(string s)
        {
            _stringBuilder.Append(s);
        }

        public void WriteIndent(bool condition = true)
        {
            if (condition && _indent > 0)
            {
                _stringBuilder.Append(' ', 2 * _indent);
            }
        }

        public void WriteLine(bool condition = true)
        {
            if (condition)
            {
                _stringBuilder.AppendLine();
            }
        }

        public void WriteSpace(bool condition = true)
        {
            if (condition)
            {
                _stringBuilder.Append(' ');
            }
        }

        public void Clear()
        {
            _stringBuilder.Clear();
        }

        public override string ToString()
        {
            return _stringBuilder.ToString();
        }
    }
}
