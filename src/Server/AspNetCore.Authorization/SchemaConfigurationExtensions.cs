#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Authorization
#else
namespace HotChocolate.AspNetCore.Authorization
#endif
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
