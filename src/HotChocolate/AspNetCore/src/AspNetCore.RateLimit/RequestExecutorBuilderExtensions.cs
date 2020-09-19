using System;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.RateLimit
{
    public static class RequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddLimitDirectiveType(
            this IRequestExecutorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddDirectiveType<LimitDirectiveType>();
        }
    }
}
