using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.Options;

namespace HotChocolate.Execution.Configuration
{
    public class RequestExecutorFactoryOptions
    {
        public ISchema? Schema { get; set; }

        public ISchemaBuilder? SchemaBuilder { get; set; }

        public RequestExecutorOptions? RequestExecutorOptions { get; set; }

        public IList<SchemaBuilderAction> SchemaBuilderActions { get; } =
            new List<SchemaBuilderAction>();

        public IList<RequestExecutorOptionsAction> RequestExecutorOptionsActions { get; } =
            new List<RequestExecutorOptionsAction>();

        public IList<RequestCoreMiddleware> Pipeline { get; } =
            new List<RequestCoreMiddleware>();

        public IList<Action<IServiceCollection>> SchemaServices { get; } =
            new List<Action<IServiceCollection>>();

        public IList<OnRequestExecutorCreatedAction> OnRequestExecutorCreated { get; } =
            new List<OnRequestExecutorCreatedAction>();

        public IList<OnRequestExecutorEvictedAction> OnRequestExecutorEvicted { get; } =
            new List<OnRequestExecutorEvictedAction>();
    }

    /// <summary>
    /// Provides dynamic configurations.
    /// </summary>
    public interface IRequestExecutorOptionsProvider
    {
        /// <summary>
        /// Gets named configuration options.
        /// </summary>
        /// <param name="cancellationToken">
        /// The <see cref="CancellationToken"/>.
        /// </param>
        /// <returns>
        /// Returns the configuration options of this provider.
        /// </returns>
        ValueTask<IEnumerable<NamedRequestExecutorFactoryOptions>> GetOptionsAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// Registers a listener to be called whenever a named
        /// <see cref="RequestExecutorFactoryOptions"/> changes.
        /// </summary>
        /// <param name="listener">
        /// The action to be invoked when <see cref="RequestExecutorFactoryOptions"/> has changed.
        /// </param>
        /// <returns>
        /// An <see cref="IDisposable"/> which should be disposed to stop listening for changes.
        /// </returns>
        IDisposable OnChange(Action<INamedRequestExecutorFactoryOptions> listener);
    }

    /// <summary>
    /// Represents something that configures the <see cref="RequestExecutorFactoryOptions"/>.
    /// </summary>
    public interface INamedRequestExecutorFactoryOptions
        : IConfigureOptions<RequestExecutorFactoryOptions>
    {
        /// <summary>
        /// The schema name to which this instance provides configurations to.
        /// </summary>
        NameString SchemaName { get; }
    }

    public sealed class NamedRequestExecutorFactoryOptions
        : INamedRequestExecutorFactoryOptions
    {
        private readonly Action<RequestExecutorFactoryOptions> _configure;

        public NamedRequestExecutorFactoryOptions(
            NameString schemaName,
            Action<RequestExecutorFactoryOptions> configure)
        {
            SchemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _configure = configure ?? throw new ArgumentNullException(nameof(configure));
        }

        public NameString SchemaName { get; }

        public void Configure(RequestExecutorFactoryOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _configure(options);
        }
    }
}
