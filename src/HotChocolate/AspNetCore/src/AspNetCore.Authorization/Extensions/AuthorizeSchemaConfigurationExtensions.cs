using HotChocolate.AspNetCore.Authorization;
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
