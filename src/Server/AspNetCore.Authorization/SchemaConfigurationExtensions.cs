#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Authorization;
#else
using HotChocolate.AspNetCore.Authorization;
#endif

namespace HotChocolate
{
    public static class SchemaConfigurationExtensions
    {
        public static void RegisterAuthorizeDirectiveType(
            this ISchemaConfiguration configuration)
        {
            configuration.RegisterDirective<AuthorizeDirectiveType>();
        }
    }
}
