namespace Prometheus.Language
{
    public class NameTokenReader
        : TokenReaderBase
    {
        public NameTokenReader(ReadNextToken readNextTokenDelegate)
            : base(readNextTokenDelegate)
        {
        }

        public override bool CanHandle(ILexerContext context)
        {
            return context.PeekTest(c => c.IsLetter() || c.IsUnderscore());
        }

        public override Token ReadToken(ILexerContext context, Token previous)
        {
            int start = context.Position;

            while (context.PeekTest(c => c.IsLetterOrDigit() || c.IsUnderscore()))
            {
                context.Read();
            }

            return CreateToken(context, previous, TokenKind.Name,
                start, context.Read(start, context.Position));
        }
    }
}