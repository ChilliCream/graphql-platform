using System;

namespace Prometheus.Language
{
    public class ParserContext
        : IParserContext
    {
        public ParserContext(ISource source, Token start)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (start == null)
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (start.Kind != TokenKind.StartOfFile)
            {
                throw new ArgumentException(
                    "start must be a start of file token.",
                    nameof(start));
            }

            Source = source;
            Current = start;
        }

        public ISource Source { get; }

        public Token Current { get; private set; }

        public bool MoveNext()
        {
            if (Current.Kind == TokenKind.EndOfFile)
            {
                return false;
            }

            Current = Peek();
            return true;
        }

        public Token Peek()
        {
            return Current.Peek();
        }
    }
}