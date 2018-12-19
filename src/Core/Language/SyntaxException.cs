using System;

namespace HotChocolate.Language
{
#if netstandard1_4
    [Serializable]
#endif
    public class SyntaxException
        : Exception
    {
        internal SyntaxException(LexerState context, string message)
            : base(message)
        {
            Position = context.Position;
            Line = context.Line;
            Column = context.Column;
            SourceText = context.SourceText;
        }

        internal SyntaxException(LexerState context,
            string message, Exception innerException)
            : base(message, innerException)
        {
            Position = context.Position;
            Line = context.Line;
            Column = context.Column;
            SourceText = context.SourceText;
        }

        internal SyntaxException(ParserContext context, string message)
            : base(message)
        {
            Position = context.Current.Start;
            Line = context.Current.Line;
            Column = context.Current.Column;
            SourceText = context.Source.Text;
        }
        internal SyntaxException(
            ParserContext context, SyntaxToken token, string message)
            : base(message)
        {
            Position = token.Start;
            Line = token.Line;
            Column = token.Column;
            SourceText = context.Source.Text;
        }

        internal SyntaxException(
            SyntaxToken token, string message)
            : base(message)
        {
            Position = token.Start;
            Line = token.Line;
            Column = token.Column;
        }

        public int Position { get; }
        public int Line { get; }
        public int Column { get; }
        public string SourceText { get; }
    }
}
