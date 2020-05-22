using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using HotChocolate.Execution.Configuration;

namespace HotChocolate.Execution
{
    internal sealed class RequestExecutorResolver : IRequestExecutorResolver
    {
        private readonly string _defaultName = "Schema_" + Guid.NewGuid().ToString("N");
        private readonly ConcurrentDictionary<string, IRequestExecutor> _executors =
            new ConcurrentDictionary<string, IRequestExecutor>();
        private readonly IOptionsMonitor<RequestExecutorFactoryOptions> _optionsMonitor;
        private readonly CreateRequestExecutor _requestExecutorFactory;
        private readonly IServiceProvider _serviceProvider;

        public event EventHandler<RequestExecutorEvictedEventArgs>? RequestExecutorEvicted;

        public RequestExecutorResolver(
            IOptionsMonitor<RequestExecutorFactoryOptions> optionsMonitor,
            CreateRequestExecutor requestExecutorFactory,
            IServiceProvider serviceProvider)
        {
            _optionsMonitor = optionsMonitor;
            _requestExecutorFactory = requestExecutorFactory;
            _serviceProvider = serviceProvider;
            _optionsMonitor.OnChange((options, name) => EvictRequestExecutor(name));
        }

        public async ValueTask<IRequestExecutor> GetRequestExecutorAsync(
            string? name = null,
            CancellationToken cancellationToken = default)
        {
            RequestExecutorFactoryOptions options = _optionsMonitor.Get(name ?? _defaultName);

            SchemaBuilder schemaBuilder = options.SchemaBuilder ?? SchemaBuilder.New();

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

            RequestDelegate next = context =>
            {
                return default;
            };

            for (int i = options.Pipeline.Count; i >= 0; i--)
            {
                next = options.Pipeline[i](_serviceProvider, next);
            }

            return _requestExecutorFactory(schemaBuilder.Create(), next);
        }

        public void EvictRequestExecutor(string? name = null)
        {
            string n = name ?? _defaultName;
            if (_executors.TryRemove(n, out IRequestExecutor? executor))
            {
                RequestExecutorEvicted?.Invoke(
                    this,
                    new RequestExecutorEvictedEventArgs(n, executor));
            }
        }
    }
}