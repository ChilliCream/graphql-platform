using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using HotChocolate;
using HotChocolate.Execution.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
    /// </summary>
    public static class RequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ISchema"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="configureSchema">A delegate that is used to configure an <see cref="ISchema"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder ConfigureSchema(
            this IRequestExecutorBuilder builder,
            Action<ISchemaBuilder> configureSchema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureSchema == null)
            {
                throw new ArgumentNullException(nameof(configureSchema));
            }

            builder.Services.Configure<RequestExecutorFactoryOptions>(
                builder.Name,
                options => options.SchemaBuilderActions.Add(
                    new SchemaBuilderAction(configureSchema)));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ISchema"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="configureSchema">A delegate that is used to configure an <see cref="ISchema"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder ConfigureSchemaAsync(
            this IRequestExecutorBuilder builder,
            Func<ISchemaBuilder, CancellationToken, ValueTask> configureSchema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureSchema == null)
            {
                throw new ArgumentNullException(nameof(configureSchema));
            }

            builder.Services.Configure<RequestExecutorFactoryOptions>(
                builder.Name,
                options => options.SchemaBuilderActions.Add(
                    new SchemaBuilderAction(configureSchema)));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ISchema"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="configureSchema">A delegate that is used to configure an <see cref="ISchema"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureSchema"/> will be the
        /// same application's root service provider instance.
        /// </remarks>
        public static IRequestExecutorBuilder ConfigureSchema(
            this IRequestExecutorBuilder builder,
            Action<IServiceProvider, ISchemaBuilder> configureSchema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureSchema == null)
            {
                throw new ArgumentNullException(nameof(configureSchema));
            }

            builder.Services.AddTransient<IConfigureOptions<RequestExecutorFactoryOptions>>(services =>
            {
                return new ConfigureNamedOptions<RequestExecutorFactoryOptions>(builder.Name, (options) =>
                {
                    options.SchemaBuilderActions.Add(
                        new SchemaBuilderAction(b => configureSchema(services, b)));
                });
            });

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ISchema"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="configureSchema">A delegate that is used to configure an <see cref="ISchema"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureSchema"/> will be the
        /// same application's root service provider instance.
        /// </remarks>
        public static IRequestExecutorBuilder ConfigureSchemaAsync(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, ISchemaBuilder, CancellationToken, ValueTask> configureSchema)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureSchema == null)
            {
                throw new ArgumentNullException(nameof(configureSchema));
            }

            builder.Services.AddTransient<IConfigureOptions<RequestExecutorFactoryOptions>>(services =>
            {
                return new ConfigureNamedOptions<RequestExecutorFactoryOptions>(builder.Name, (options) =>
                {
                    options.SchemaBuilderActions.Add(
                        new SchemaBuilderAction((b, ct) => configureSchema(services, b, ct)));
                });
            });

            return builder;
        }
    }
}