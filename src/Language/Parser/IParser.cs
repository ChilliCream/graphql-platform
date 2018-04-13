namespace HotChocolate.Language
{
    public interface IParser
    {
        DocumentNode Parse(ILexer lexer, ISource source);

        DocumentNode Parse(ILexer lexer, ISource source, ParserOptions options);
    }
}
