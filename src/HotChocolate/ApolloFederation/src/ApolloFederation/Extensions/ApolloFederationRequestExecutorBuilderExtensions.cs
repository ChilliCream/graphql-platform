using System;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.ApolloFederation.Extensions;

/// <summary>
/// Provides extensions to <see cref="IRequestExecutorBuilder"/>.
/// </summary>
public static class ApolloFederationRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds support for Apollo Federation to the schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddApolloFederation(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.ConfigureSchema(s => ApolloFederationSchemaBuilderExtensions.AddApolloFederation(s));
    }
}
