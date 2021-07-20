using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;

namespace Microsoft.Extensions.DependencyInjection
{
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

        public static IRequestExecutorBuilder AddGlobalObjectIdentification(
            this IRequestExecutorBuilder builder)
            => builder.ConfigureSchema(c => c.AddGlobalObjectIdentification());

        public static IRequestExecutorBuilder AddQueryFieldToMutationPayloads(
            this IRequestExecutorBuilder builder,
            Action<MutationPayloadOptions>? configureOptions = null)
            => builder.ConfigureSchema(c => c.AddQueryFieldToMutationPayloads(configureOptions));
    }
}
