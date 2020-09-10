using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateAuthorizeRequestExecutorBuilder
    {
        public static IRequestExecutorBuilder AddAuthorizeDirectiveType(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(sb => sb.AddDirectiveType<AuthorizeDirectiveType>());
        }
    }
}
