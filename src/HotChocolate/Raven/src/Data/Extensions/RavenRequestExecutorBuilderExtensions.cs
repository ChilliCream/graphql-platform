using HotChocolate.Data.Raven;
using HotChocolate.Execution.Configuration;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static class RavenRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Registers a well-known <see cref="DocumentStore"/> with the resolver compiler.
    /// The <see cref="IAsyncDocumentSession"/> does no longer need any annotation in the resolver.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder RegisterDocumentStore(
        this IRequestExecutorBuilder builder)
    {
        builder.Services.TryAddParameterExpressionBuilder<DocumentStoreParameterExpressionBuilder>();

        return builder;
    }
}
