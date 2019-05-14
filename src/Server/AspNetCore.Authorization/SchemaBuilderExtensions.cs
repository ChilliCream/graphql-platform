#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Authorization;
#else
using HotChocolate.AspNetCore.Authorization;
#endif
using HotChocolate.Configuration;

namespace HotChocolate
{
    public static class SchemaBuilderExtensions
    {
        public static ISchemaBuilder AddAuthorizeDirectiveType(
            this ISchemaBuilder builder)
        {
            return builder.AddDirectiveType<AuthorizeDirectiveType>();
        }
    }
}
