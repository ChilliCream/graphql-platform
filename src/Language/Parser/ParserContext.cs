using System;

namespace HotChocolate.Language
{
    internal sealed class ParserContext
    {
        public ParserContext(ISource source, SyntaxToken start, ParserOptions options)
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

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Source = source;
            Current = start;
            Options = options;
        }

        public ParserOptions Options { get; }
        public ISource Source { get; }
        public SyntaxToken Current { get; private set; }

        public bool MoveNext()
        {
            if (Current.Kind == TokenKind.EndOfFile)
            {
                return false;
            }

            Current = Current.Peek();
            return true;
        }
    }
}