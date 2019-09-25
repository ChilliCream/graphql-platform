using HotChocolate.Language;

#if ASPNETCLASSIC
using Microsoft.Owin;
using HotChocolate.AspNetClassic.Interceptors;
#else
#endif

#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    public interface IParserOptionsAccessor
    {
        ParserOptions ParserOptions { get; }
    }
}
