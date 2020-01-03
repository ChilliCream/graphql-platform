using System;

namespace StrawberryShake.Language
{
    [Serializable]
    public class SyntaxException
        : Exception
    {
        internal unsafe SyntaxException(TextGraphQLReader reader, string message)
            : base(message)
        {
            Position = reader.Position;
            Line = reader.Line;
            Column = reader.Column;
            fixed (char* c = reader.GraphQLData)
            {
                SourceText = new string(c);
            }
        }

        public int Position { get; }

        public int Line { get; }

        public int Column { get; }

        public string SourceText { get; }
    }
}
