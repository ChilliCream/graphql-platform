using System;

namespace Prometheus.Language
{
    public interface IParserContext
    {
        ISource Source { get; }

        Token Current { get; }

        bool MoveNext();

        Token Peek();
    }

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

            Current = Current.Next;
            return true;
        }

        public Token Peek()
        {
            return Current.Next;
        }
    }


}