using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
    /// </summary>
    public static partial class RequestExecutorBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ISchema"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="configureSchema">
        /// A delegate that is used to configure an <see cref="ISchema"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        public static IRequestExecutorBuilder ConfigureSchema(
            this IRequestExecutorBuilder builder,
            Action<ISchemaBuilder> configureSchema)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureSchema is null)
            {
                throw new ArgumentNullException(nameof(configureSchema));
            }

            return Configure(
                builder,
                options => options.SchemaBuilderActions.Add(
                    new SchemaBuilderAction(configureSchema)));
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ISchema"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="configureSchema">
        /// A delegate that is used to configure an <see cref="ISchema"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        public static IRequestExecutorBuilder ConfigureSchemaAsync(
            this IRequestExecutorBuilder builder,
            Func<ISchemaBuilder, CancellationToken, ValueTask> configureSchema)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureSchema is null)
            {
                throw new ArgumentNullException(nameof(configureSchema));
            }

            return Configure(
                builder,
                options => options.SchemaBuilderActions.Add(
                    new SchemaBuilderAction(configureSchema)));
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ISchema"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="configureSchema">
        /// A delegate that is used to configure an <see cref="ISchema"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureSchema"/>
        /// will be the same application's root service provider instance.
        /// </remarks>
        public static IRequestExecutorBuilder ConfigureSchema(
            this IRequestExecutorBuilder builder,
            Action<IServiceProvider, ISchemaBuilder> configureSchema)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureSchema is null)
            {
                throw new ArgumentNullException(nameof(configureSchema));
            }

            return Configure(
                builder,
                (services, options) => options.SchemaBuilderActions.Add(
                    new SchemaBuilderAction(b => configureSchema(services, b))));
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ISchema"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="configureSchema">
        /// A delegate that is used to configure an <see cref="ISchema"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureSchema"/>
        /// will be the same application's root service provider instance.
        /// </remarks>
        public static IRequestExecutorBuilder ConfigureSchemaAsync(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, ISchemaBuilder, CancellationToken, ValueTask> configureSchema)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureSchema is null)
            {
                throw new ArgumentNullException(nameof(configureSchema));
            }

            return Configure(
                builder,
                (services, options) => options.SchemaBuilderActions.Add(
                    new SchemaBuilderAction((b, ct) => configureSchema(services, b, ct))));
        }

        /// <summary>
        /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="modify">
        /// A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
        /// and its execution.
        /// </returns>
        public static IRequestExecutorBuilder ModifyRequestOptions(
            this IRequestExecutorBuilder builder,
            Action<RequestExecutorOptions> modify)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (modify is null)
            {
                throw new ArgumentNullException(nameof(modify));
            }

            return Configure(
                builder,
                options => options.RequestExecutorOptionsActions.Add(
                    new RequestExecutorOptionsAction(modify)));
        }

        /// <summary>
        /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="modify">A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder ModifyRequestOptionsAsync(
            this IRequestExecutorBuilder builder,
            Func<RequestExecutorOptions, CancellationToken, ValueTask> modify)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (modify is null)
            {
                throw new ArgumentNullException(nameof(modify));
            }

            return Configure(
                builder,
                options => options.RequestExecutorOptionsActions.Add(
                    new RequestExecutorOptionsAction(modify)));
        }

        /// <summary>
        /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="modify">A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder ModifyRequestOptions(
            this IRequestExecutorBuilder builder,
            Action<IServiceProvider, RequestExecutorOptions> modify)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (modify is null)
            {
                throw new ArgumentNullException(nameof(modify));
            }

            return Configure(
                builder,
                (services, options) => options.RequestExecutorOptionsActions.Add(
                    new RequestExecutorOptionsAction(o => modify(services, o))));

        }

        /// <summary>
        /// Adds a delegate that will be used to modify the <see cref="RequestExecutorOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="modify">A delegate that is used to modify the <see cref="RequestExecutorOptions"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder ModifyRequestOptionsAsync(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, RequestExecutorOptions, CancellationToken, ValueTask> modify)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (modify is null)
            {
                throw new ArgumentNullException(nameof(modify));
            }

            return Configure(
                builder,
                (services, options) => options.RequestExecutorOptionsActions.Add(
                    new RequestExecutorOptionsAction((o, ct) => modify(services, o, ct))));
        }

        /// <summary>
        /// Adds a delegate that will be used to create the <see cref="RequestExecutorOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="factory">A delegate that is used to create the <see cref="RequestExecutorOptions"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder SetRequestOptions(
            this IRequestExecutorBuilder builder,
            Func<RequestExecutorOptions> factory)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return Configure(
                builder,
                options => options.RequestExecutorOptions = factory());
        }

        /// <summary>
        /// Adds a delegate that will be used to create the <see cref="RequestExecutorOptions"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRequestExecutorBuilder"/>.</param>
        /// <param name="factory">A delegate that is used to create the <see cref="RequestExecutorOptions"/>.</param>
        /// <returns>An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema and its execution.</returns>
        public static IRequestExecutorBuilder SetRequestOptions(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, RequestExecutorOptions> factory)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return Configure(
                builder,
                (services, options) => options.RequestExecutorOptions = factory(services));
        }

        public static IRequestExecutorBuilder ConfigureSchemaServices(
            this IRequestExecutorBuilder builder,
            Action<IServiceCollection> configureServices)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureServices is null)
            {
                throw new ArgumentNullException(nameof(configureServices));
            }

            return Configure(
                builder,
                options => options.SchemaServices.Add(configureServices));
        }

        public static IRequestExecutorBuilder ConfigureOnRequestExecutorCreated(
            this IRequestExecutorBuilder builder,
            Action<IRequestExecutor> action)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return builder.Configure(o => o.OnRequestExecutorCreated.Add(
                new OnRequestExecutorCreatedAction(action)));
        }

        public static IRequestExecutorBuilder ConfigureOnRequestExecutorCreated(
            this IRequestExecutorBuilder builder,
            Action<IServiceProvider, IRequestExecutor> action)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return builder.Configure((s, o) => o.OnRequestExecutorCreated.Add(
                new OnRequestExecutorCreatedAction(e => action(s, e))));
        }

        public static IRequestExecutorBuilder ConfigureOnRequestExecutorCreatedAsync(
            this IRequestExecutorBuilder builder,
            Func<IRequestExecutor, CancellationToken, ValueTask> asyncAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (asyncAction == null)
            {
                throw new ArgumentNullException(nameof(asyncAction));
            }

            return builder.Configure(o => o.OnRequestExecutorCreated.Add(
                new OnRequestExecutorCreatedAction(asyncAction)));
        }

        public static IRequestExecutorBuilder ConfigureOnRequestExecutorCreatedAsync(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, IRequestExecutor, CancellationToken, ValueTask> asyncAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (asyncAction == null)
            {
                throw new ArgumentNullException(nameof(asyncAction));
            }

            return builder.Configure((s, o) => o.OnRequestExecutorCreated.Add(
                new OnRequestExecutorCreatedAction((e, ct) => asyncAction(s, e, ct))));
        }

        internal static IRequestExecutorBuilder Configure(
            this IRequestExecutorBuilder builder,
            Action<RequestExecutorFactoryOptions> configure)
        {
            builder.Services.Configure(builder.Name, configure);
            return builder;
        }

        internal static IRequestExecutorBuilder Configure(
            this IRequestExecutorBuilder builder,
            Action<IServiceProvider, RequestExecutorFactoryOptions> configure)
        {
            builder.Services.AddTransient<IConfigureOptions<RequestExecutorFactoryOptions>>(
                services => new ConfigureNamedOptions<RequestExecutorFactoryOptions>(
                    builder.Name,
                    options => configure(services, options)));

            return builder;
        }
    }
}
