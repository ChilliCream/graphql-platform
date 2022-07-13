using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Enables relay schema style.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="options">
    /// The relay schema options.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    [Obsolete("Use AddGlobalObjectIdentification / AddQueryFieldToMutationPayloads")]
    public static IRequestExecutorBuilder EnableRelaySupport(
        this IRequestExecutorBuilder builder,
        RelayOptions? options = null) =>
        builder.ConfigureSchema(c => c.EnableRelaySupport(options));

    /// <summary>
    /// Adds a <c>node</c> field to the root query according to the
    /// Global Object Identification specification.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AddGlobalObjectIdentification(
        this IRequestExecutorBuilder builder)
        => builder.ConfigureSchema(c => c.AddGlobalObjectIdentification());

    /// <summary>
    /// Adds a <c>node</c> field to the root query according to the
    /// Global Object Identification specification.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
    /// <param name="registerNodeInterface">
    /// Specifies if the node interface and the node field shall be registered with the schema.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AddGlobalObjectIdentification(
        this IRequestExecutorBuilder builder,
        bool registerNodeInterface)
        => builder.ConfigureSchema(c => c.AddGlobalObjectIdentification(registerNodeInterface));

    /// <summary>
    /// Enables rewriting of mutation payloads to provide access to a query root field.
    /// </summary>
    /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
    /// <param name="configureOptions">
    /// A delegate to configure the <see cref="MutationPayloadOptions"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder AddQueryFieldToMutationPayloads(
        this IRequestExecutorBuilder builder,
        Action<MutationPayloadOptions>? configureOptions = null)
        => builder.ConfigureSchema(c => c.AddQueryFieldToMutationPayloads(configureOptions));
}
