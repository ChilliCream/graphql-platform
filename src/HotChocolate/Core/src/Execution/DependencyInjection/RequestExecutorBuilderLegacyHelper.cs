using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration
{
    /// <summary>
    /// Helper to enable legacy configurations.
    /// </summary>
    public static class RequestExecutorBuilderLegacyHelper
    {
        /// <summary>
        /// Sets the schema builder that shall be used to configure the request executor.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="schemaBuilder">
        /// The schema builder that shall be used to configure the request executor.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        [Obsolete(
            "This helper only exists to allow legacy schema handling. " +
            "Consider moving to the new configuration API.")]
        public static IRequestExecutorBuilder SetSchemaBuilder(
            IRequestExecutorBuilder builder,
            ISchemaBuilder schemaBuilder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (schemaBuilder is null)
            {
                throw new ArgumentNullException(nameof(schemaBuilder));
            }

            return builder.Configure(options => options.SchemaBuilder = schemaBuilder);
        }

        /// <summary>
        /// Sets the schema builder that shall be used to configure the request executor.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="schema">
        /// The schema that shall be used to configure the request executor.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        [Obsolete(
            "This helper only exists to allow legacy schema handling. " +
            "Consider moving to the new configuration API.")]
        public static IRequestExecutorBuilder SetSchema(
            IRequestExecutorBuilder builder,
            ISchema schema)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (schema is null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            return builder.Configure(options => options.Schema = schema);
        }

        /// <summary>
        /// Sets the schema builder that shall be used to configure the request executor.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="schemaFactory">
        /// The factory to create the schema.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        [Obsolete(
            "This helper only exists to allow legacy schema handling. " +
            "Consider moving to the new configuration API.")]
        public static IRequestExecutorBuilder SetSchema(
            IRequestExecutorBuilder builder,
            Func<IServiceProvider, ISchema> schemaFactory)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (schemaFactory is null)
            {
                throw new ArgumentNullException(nameof(schemaFactory));
            }

            return builder.Configure((s, o) => o.Schema = schemaFactory(s));
        }
    }
}
