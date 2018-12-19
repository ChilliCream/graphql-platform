#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic
#else
namespace HotChocolate.AspNetCore
#endif
{
    internal static class ContentType
    {
        public const string GraphQL = "application/graphql";

        public const string Json = "application/json";
    }
}
