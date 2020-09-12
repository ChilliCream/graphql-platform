using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateCursorPaginationRequestExecutorBuilderExtension
    {
        public static IRequestExecutorBuilder SetConnectionSettings(
            this IRequestExecutorBuilder builder,
            ConnectionSettings settings)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(s => s.SetConnectionSettings(settings));
        }
    }
}
