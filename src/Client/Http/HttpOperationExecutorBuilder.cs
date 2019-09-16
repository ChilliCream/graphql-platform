using System.Collections.Generic;
using System.Net.Http;
using System;
using StrawberryShake.Http.Pipelines;
using System.Linq;
using StrawberryShake.Http.Utilities;

namespace StrawberryShake.Http
{
    public class HttpOperationExecutorBuilder
        : IHttpOperationExecutorBuilder
    {
        private readonly Dictionary<Type, List<object>> _services =
            new Dictionary<Type, List<object>>();
        private readonly List<IServiceProvider> _serviceProviders =
            new List<IServiceProvider>();
        private HttpOperationExecutionPipelineBuilder _pipelineBuilder =
            HttpOperationExecutionPipelineBuilder.New();
        private HttpClient? _client;

        public IHttpOperationExecutorBuilder AddService(Type serviceType, object serviceInstance)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (serviceInstance is null)
            {
                throw new ArgumentNullException(nameof(serviceInstance));
            }

            if (!_services.TryGetValue(serviceType, out List<object>? services))
            {
                services = new List<object>();
                _services.Add(serviceType, services);
            }
            services.Add(serviceInstance);
            return this;
        }

        public IHttpOperationExecutorBuilder AddServices(IServiceProvider services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _serviceProviders.Add(services);
            return this;
        }

        public IHttpOperationExecutorBuilder SetClient(HttpClient client)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _client = client;
            return this;
        }

        public IHttpOperationExecutorBuilder Use(
            Func<OperationDelegate, OperationDelegate> middleware)
        {
            if (middleware is null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _pipelineBuilder.Use(middleware);
            return this;
        }

        public IHttpOperationExecutorBuilder Use(OperationMiddleware middleware)
        {
            _pipelineBuilder.Use(middleware);
            return this;
        }

        public IOperationExecutor Build()
        {
            if (_client is null)
            {
                throw new InvalidOperationException(
                    "The HTTP client must be provided in order to create an executor.");
            }

            IServiceProvider services = BuildServices();

            OperationDelegate executeOperation = _pipelineBuilder.Build(services);

            return new HttpOperationExecutor(_client, executeOperation, services);
        }

        private IServiceProvider BuildServices()
        {
            IServiceProvider? services = null;

            while (_serviceProviders.Count > 0)
            {
                if (services == null)
                {
                    services = _serviceProviders.Last();
                }
                else
                {
                    services = new CombinedServiceProvider(
                        _serviceProviders.Last(),
                        services);
                }
                _serviceProviders.RemoveAt(_serviceProviders.Count - 1);
            }

            if (_services.Count > 0)
            {
                var localServices = new DictionaryServiceProvider(_services);
                return services is null
                    ? (IServiceProvider)localServices
                    : new CombinedServiceProvider(localServices, services);
            }

            if (services is null)
            {
                throw new InvalidOperationException(
                    "There was no service provider or service specified.");
            }

            return services;
        }

        public static HttpOperationExecutorBuilder New() =>
            new HttpOperationExecutorBuilder();
    }
}
