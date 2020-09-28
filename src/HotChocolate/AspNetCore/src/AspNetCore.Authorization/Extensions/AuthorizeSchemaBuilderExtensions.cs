using System;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate
{
    public static class AuthorizeSchemaBuilderExtensions
    {
        public static ISchemaBuilder AddAuthorizeDirectiveType(
            this ISchemaBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddDirectiveType<AuthorizeDirectiveType>();
        }
    }
}
