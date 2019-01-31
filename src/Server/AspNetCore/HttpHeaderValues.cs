#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    internal static class HttpHeaderValues
    {
        public const string TracingEnabled = "1";
    }
}
