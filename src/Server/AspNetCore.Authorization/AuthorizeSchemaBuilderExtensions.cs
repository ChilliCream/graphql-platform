using System;
#if ASPNETCLASSIC
using HotChocolate.AspNetClassic.Authorization;
#else
using HotChocolate.AspNetCore.Authorization;
#endif
using HotChocolate.Configuration;

namespace HotChocolate
{
    public static class AuthorizeSchemaBuilderExtensions
    {
        public static ISchemaBuilder AddAuthorizeDirectiveType(
            this ISchemaBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddDirectiveType<AuthorizeDirectiveType>();
        }
    }
}
