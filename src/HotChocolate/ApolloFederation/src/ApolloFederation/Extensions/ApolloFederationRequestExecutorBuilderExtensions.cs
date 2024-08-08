using HotChocolate.ApolloFederation;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Execution.Configuration;
using FederationVersion = HotChocolate.ApolloFederation.FederationVersion;

namespace Microsoft.Extensions.DependencyInjection;

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
    /// <param name="version">
    /// The apollo federation version to use.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddApolloFederation(
        this IRequestExecutorBuilder builder,
        FederationVersion version = FederationVersion.Default)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.SetContextData(FederationContextData.FederationVersion, version);
        builder.TryAddTypeInterceptor<FederationTypeInterceptor>();
        builder.BindRuntimeType<Policy, StringType>();
        builder.AddTypeConverter<Policy, string>(from => from.Value);
        builder.AddTypeConverter<string, Policy>(from => new Policy(from));
        builder.BindRuntimeType<Scope, StringType>();
        builder.AddTypeConverter<Scope, string>(from => from.Value);
        builder.AddTypeConverter<string, Scope>(from => new Scope(from));
        return builder;
    }
}
