using System;
using System.Text;

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

        internal SyntaxException(TextGraphQLReader reader, string message)
            : base(message)
        {
            Position = reader.Position;
            Line = reader.Line;
            Column = reader.Column;
            SourceText = Encoding.UTF8.GetString(reader.GraphQLData.ToArray());
        }

        public int Position { get; }

        public int Line { get; }

        public int Column { get; }

        public string SourceText { get; }
    }
}
