#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    internal static class HttpHeaderKeys
    {
        public const string Tracing = "GraphQL-Tracing";
    }
}
