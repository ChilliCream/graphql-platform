#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Authorization;
#else
using HotChocolate.AspNetCore.Authorization;
#endif
using HotChocolate.Configuration;

namespace HotChocolate
{
    public static class AuthorizeSchemaConfigurationExtensions
    {
        public static void RegisterAuthorizeDirectiveType(
            this ISchemaConfiguration configuration)
        {
            configuration.RegisterDirective<AuthorizeDirectiveType>();
        }
    }
}
