using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.AspNetCore.Authorization;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateAuthorizeRequestExecutorBuilder
    {
        /// <summary>
        /// Adds the authorization types to the schema.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/> for chaining in more configurations.
        /// </returns>
        public static IRequestExecutorBuilder AddAuthorization(
            this IRequestExecutorBuilder builder) =>
            builder.ConfigureSchema(sb => sb.AddAuthorizeDirectiveType());

        [Obsolete("Use AddAuthorization()")]
        public static IRequestExecutorBuilder AddAuthorizeDirectiveType(
            this IRequestExecutorBuilder builder) =>
            AddAuthorization(builder);
    }
}
