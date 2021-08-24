using System.Text;
using System;

namespace HotChocolate.Language
{
    [Serializable]
    public class SyntaxException
        : Exception
    {
        internal SyntaxException(Utf8GraphQLReader reader, string message)
            : base(message)
        {
            Position = reader.Position;
            Line = reader.Line;
            Column = reader.Column;
            SourceText = Encoding.UTF8.GetString(reader.GraphQLData.ToArray());
        }

        internal SyntaxException(Utf8GraphQLReader reader, string message, params object[] args)
            : this(reader, string.Format(message, args))
        {
        }

        public int Position { get; }

        public int Line { get; }

        public int Column { get; }

        public string SourceText { get; }
    }
}
