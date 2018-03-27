using System;

namespace Prometheus.Language
{
    public abstract class TokenReaderBase
        : ITokenReader
    {
        private ReadNextToken _readNextTokenDelegate;

        public TokenReaderBase(ReadNextToken readNextTokenDelegate)
        {
            if (readNextTokenDelegate == null)
            {
                throw new ArgumentNullException(nameof(readNextTokenDelegate));
            }
            _readNextTokenDelegate = readNextTokenDelegate;
        }

		public abstract bool CanHandle(ILexerContext context);

		public abstract Token ReadToken(ILexerContext context, Token previous);

        protected Token CreateToken(ILexerContext context, Token previous,
            TokenKind kind, int start, string value)
        {
            NextTokenThunk next = CreateNextThunk(context);
            Token token = new Token(kind, start, context.Position,
                context.Line, context.Column, value, previous, next);
            next.SetPrevious(token);
            return token;
        }

        protected Token CreateToken(ILexerContext context, Token previous,
            TokenKind kind, int start)
        {
            NextTokenThunk next = CreateNextThunk(context);
            Token token = new Token(kind, start, context.Position,
                context.Line, context.Column, previous, next);
            next.SetPrevious(token);
            return token;
        }

        private NextTokenThunk CreateNextThunk(ILexerContext context)
        {
            return new NextTokenThunk(previous => _readNextTokenDelegate(context, previous));
        }
    }
}