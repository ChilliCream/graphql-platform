using System;

namespace HotChocolate.Language
{
    internal sealed class ParserContext
    {
        public ParserContext(
            ISource source,
            SyntaxToken start,
            ParserOptions options,
            Func<ParserContext, NameNode> parseName)
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

            if (parseName == null)
            {
                throw new ArgumentNullException(nameof(parseName));
            }

            Source = source;
            Current = start;
            Options = options;
            ParseName = () => parseName(this);
        }

        public ParserOptions Options { get; }

        public ISource Source { get; }

        public SyntaxToken Current { get; private set; }

        public Func<NameNode> ParseName { get; }

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
