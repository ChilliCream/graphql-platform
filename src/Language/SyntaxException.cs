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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Position = context.Position;
            Line = context.Line;
            LineStart = context.LineStart;
            Column = context.Column;
            SourceText = context.SourceText;
        }

        internal SyntaxException(LexerState context,
            string message, Exception innerException)
            : base(message, innerException)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Position = context.Position;
            Line = context.Line;
            LineStart = context.LineStart;
            Column = context.Column;
            SourceText = context.SourceText;
        }

        internal SyntaxException(ParserContext context, string message)
        {

        }
        internal SyntaxException(ParserContext context, Token token, string message)
        {

        }

        public int Position { get; }
        public int Line { get; }
        public int LineStart { get; }
        public int Column { get; }
        public string SourceText { get; }
    }
}