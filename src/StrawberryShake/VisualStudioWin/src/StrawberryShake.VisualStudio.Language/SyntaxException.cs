using System;

namespace StrawberryShake.VisualStudio.Language
{
    [Serializable]
    public class SyntaxException
        : Exception
    {
        internal unsafe SyntaxException(StringGraphQLReader reader, string message)
            : base(message)
        {
            Position = reader.Position;
            Line = reader.Line;
            Column = reader.Column;
            fixed (char* c = reader.GraphQLData)
            {
                SourceText = new string(c, 0, reader.GraphQLData.Length);
            }
        }

        public int Position { get; }

        public int Line { get; }

        public int Column { get; }

        public string SourceText { get; }
    }
}
