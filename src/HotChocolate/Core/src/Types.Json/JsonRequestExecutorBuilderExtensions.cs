using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides JSON helpers to the schema.
/// </summary>
public static class JsonRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds JSON schema helpers to the schema.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The GraphQL configuration builder is null.
    /// </exception>
    public static IRequestExecutorBuilder AddJsonSupport(this IRequestExecutorBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureSchema(sb => sb.TryAddSchemaDirective(new FromJsonSchemaDirective()));
        return builder;
    }
}
