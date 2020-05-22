using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Errors;
using HotChocolate.Execution.Options;
using HotChocolate.Execution.Utilities;
using HotChocolate.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class RequestExecutorResolver : IRequestExecutorResolver
    {
        private readonly ConcurrentDictionary<string, IRequestExecutor> _executors =
            new ConcurrentDictionary<string, IRequestExecutor>();
        private readonly IOptionsMonitor<RequestExecutorFactoryOptions> _optionsMonitor;
        private readonly IServiceProvider _serviceProvider;

        public event EventHandler<RequestExecutorEvictedEventArgs>? RequestExecutorEvicted;

        public RequestExecutorResolver(
            IOptionsMonitor<RequestExecutorFactoryOptions> optionsMonitor,
            IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _serviceProvider = serviceProvider;
            _optionsMonitor.OnChange((options, name) => EvictRequestExecutor(name));
        }

        public async ValueTask<IRequestExecutor> GetRequestExecutorAsync(
            string? name = null,
            CancellationToken cancellationToken = default)
        {
            RequestExecutorFactoryOptions options = _optionsMonitor.Get(
                name ?? Microsoft.Extensions.Options.Options.DefaultName);

            ISchema schema = await CreateSchemaAsync(options, cancellationToken);

            RequestExecutorOptions executorOptions =
                await CreateExecutorOptionsAsync(options, cancellationToken);
            RequestDelegate pipeline = CreatePipeline(options, executorOptions);
            IEnumerable<IErrorFilter> errorFilters = CreateErrorFilters(options, executorOptions);

            return new RequestExecutor(
                schema,
                _serviceProvider,
                new ErrorHandler(errorFilters, executorOptions),
                _serviceProvider.GetRequiredService<ITypeConversion>(),
                new DefaultActivator(_serviceProvider),
                pipeline);
        }

        public void EvictRequestExecutor(string? name = null)
        {
            string n = name ?? Microsoft.Extensions.Options.Options.DefaultName;
            if (_executors.TryRemove(n, out IRequestExecutor? executor))
            {
                RequestExecutorEvicted?.Invoke(
                    this,
                    new RequestExecutorEvictedEventArgs(n, executor));
            }
        }

        private async ValueTask<ISchema> CreateSchemaAsync(
            RequestExecutorFactoryOptions options,
            CancellationToken cancellationToken)
        {
            var schemaBuilder = options.SchemaBuilder ?? new SchemaBuilder();

            foreach (SchemaBuilderAction action in options.SchemaBuilderActions)
            {
                if (action.Action is { } configure)
                {
                    configure(schemaBuilder);
                }

                if (action.AsyncAction is { } configureAsync)
                {
                    await configureAsync(schemaBuilder, cancellationToken).ConfigureAwait(false);
                }
            }

            return schemaBuilder.Create();
        }

        private async ValueTask<RequestExecutorOptions> CreateExecutorOptionsAsync(
            RequestExecutorFactoryOptions options,
            CancellationToken cancellationToken)
        {
            var executorOptions = options.RequestExecutorOptions ?? new RequestExecutorOptions();

            foreach (RequestExecutorOptionsAction action in options.RequestExecutorOptionsActions)
            {
                if (action.Action is { } configure)
                {
                    configure(executorOptions);
                }

                if (action.AsyncAction is { } configureAsync)
                {
                    await configureAsync(executorOptions, cancellationToken).ConfigureAwait(false);
                }
            }

            return executorOptions;
        }

        private RequestDelegate CreatePipeline(
            RequestExecutorFactoryOptions options,
            RequestExecutorOptions executorOptions)
        {
            if (options.Pipeline.Count == 0)
            {
                options.Pipeline.AddDefaultPipeline();
            }

            RequestDelegate next = context =>
            {
                return default;
            };

            for (int i = options.Pipeline.Count - 1; i >= 0; i--)
            {
                next = options.Pipeline[i](_serviceProvider, executorOptions, next);
            }

            return next;
        }

        private IEnumerable<IErrorFilter> CreateErrorFilters(
            RequestExecutorFactoryOptions options,
            RequestExecutorOptions executorOptions)
        {
            foreach (CreateErrorFilter factory in options.ErrorFilters)
            {
                yield return factory(_serviceProvider, executorOptions);
            }

            foreach (IErrorFilter errorFilter in _serviceProvider.GetServices<IErrorFilter>())
            {
                yield return errorFilter;
            }
        }

    }
}