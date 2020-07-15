using HotChocolate.Language;

namespace HotChocolate.AspNetCore
{
    public interface IParserOptionsAccessor
    {
        ParserOptions ParserOptions { get; }
    }
}
